using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json;

namespace Dusharp.Newtonsoft;

public sealed class DefaultUnionJsonConverter : JsonConverter
{
	private readonly ConcurrentDictionary<Type, UnionConverter> _converters = new();

	public override bool CanConvert(Type objectType)
	{
		if (!typeof(IUnion).IsAssignableFrom(objectType))
		{
			return false;
		}

		if (_converters.ContainsKey(objectType))
		{
			return true;
		}

		var hasUnionAttribute = objectType.GetCustomAttribute<UnionAttribute>(false) != null;
		if (objectType.IsValueType)
		{
			return hasUnionAttribute;
		}

		return hasUnionAttribute || objectType.BaseType?.GetCustomAttribute<UnionAttribute>() != null;
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		var unionConverter = GetConverter(objectType);
		JsonConverterHelpers.BeforeRead(reader, unionConverter.UnionType);
		var result = unionConverter.Deserializer(reader, serializer);
		JsonConverterHelpers.AfterRead(reader, result.HasParameters);

		return result.Union;
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		if (value is not IUnion union)
		{
			throw new JsonSerializationException($"Value of type {value.GetType()} is not a union");
		}

		GetConverter(value.GetType()).Serializer(writer, union, serializer);
	}

	private UnionConverter GetConverter(Type unionType) =>
		_converters.GetOrAdd(
			unionType,
			t => t.IsValueType || t.GetCustomAttribute<UnionAttribute>(false) != null
				? CreateConverter(t)
				: GetConverter(t.BaseType!));

	private static UnionConverter CreateConverter(Type unionType)
	{
		var unionCaseInfos = unionType.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Where(x => x.GetCustomAttribute<UnionCaseAttribute>() != null)
			.Select(m => new UnionCaseInfo(m))
			.ToArray();

		var serializer = CreateSerializer(unionType, unionCaseInfos);
		var deserializer = CreateDeserializer(unionType, unionCaseInfos);

		return new UnionConverter(unionType, serializer, deserializer);
	}

	private static Action<JsonWriter, IUnion, JsonSerializer> CreateSerializer(
		Type unionType, IReadOnlyCollection<UnionCaseInfo> unionCaseInfos)
	{
		var writerExpr = Expression.Parameter(typeof(JsonWriter), "writer");
		var valueExpr = Expression.Parameter(typeof(IUnion), "value");
		var serializerExpr = Expression.Parameter(typeof(JsonSerializer), "serializer");
		var returnLabel = Expression.Label();

		var castValueExpr = Expression.Convert(valueExpr, unionType);
		var serializeCasesExprs = unionCaseInfos
			.Select(Expression (unionCase) =>
			{
				var caseParameterVariableExprs = unionCase.Parameters
					.Select(p => Expression.Variable(p.ParameterType, $"{unionCase.Name}_{p.Name}"))
					.ToArray();

				var serializeCaseExpr = unionCase.Parameters.Length switch
				{
					0 => GetParameterlessCaseSerializeExpression(unionCase.Name, writerExpr),
					_ => GetMultipleParametersCaseSerializeExpression(unionCase, caseParameterVariableExprs, writerExpr,
						serializerExpr),
				};

				return Expression.Block(
					caseParameterVariableExprs,
					Expression.Condition(
						Expression.Call(castValueExpr, unionCase.TryGetDataMethod, caseParameterVariableExprs),
						Expression.Block(serializeCaseExpr, Expression.Return(returnLabel)),
						Expression.Empty()));
			})
			.ToArray();

		var serializerBody = Expression.Block([
			..serializeCasesExprs,
			Expression.Call(null, JsonConverterHelpers.ThrowUnionInInvalidStateMethodInfo,
				Expression.Constant(unionType), Expression.Constant(valueExpr.Name)),
			Expression.Label(returnLabel),
		]);

		return Expression
			.Lambda<Action<JsonWriter, IUnion, JsonSerializer>>(serializerBody, writerExpr, valueExpr, serializerExpr)
			.Compile();
	}

	private static Expression GetParameterlessCaseSerializeExpression(
		string unionCaseName, ParameterExpression writerExpr) =>
		Expression.Call(writerExpr, JsonConverterHelpers.WriteStringValueMethodInfo, Expression.Constant(unionCaseName));

	private static Expression GetMultipleParametersCaseSerializeExpression(
		UnionCaseInfo unionCase, ParameterExpression[] caseParameterVariableExprs, ParameterExpression writerExpr,
		ParameterExpression jsonSerializerExpr) =>
		Expression.Block([
			Expression.Call(writerExpr, JsonConverterHelpers.WriteStartObjectMethodInfo),
			Expression.Call(null, JsonConverterHelpers.WriteStartObjectWithPropertyMethodInfo, writerExpr,
				Expression.Constant(unionCase.Name)),
			..unionCase.Parameters
				.Zip(caseParameterVariableExprs, (x, y) => (x, y))
				.Select(Expression (x) =>
				{
					var (parameter, variable) = x;
					var writePropertyMethodInfo = JsonConverterHelpers.WritePropertyGenericMethodInfo
						.MakeGenericMethod(parameter.ParameterType);

					return Expression.Call(null, writePropertyMethodInfo, writerExpr,
						Expression.Constant(parameter.Name), variable, jsonSerializerExpr);
				}),
			Expression.Call(writerExpr, JsonConverterHelpers.WriteEndObjectMethodInfo),
			Expression.Call(writerExpr, JsonConverterHelpers.WriteEndObjectMethodInfo),
		]);

	private static Func<JsonReader, JsonSerializer, DeserializeResult> CreateDeserializer(
		Type unionType, IReadOnlyCollection<UnionCaseInfo> unionCaseInfos)
	{
		var readerExpr = Expression.Parameter(typeof(JsonReader), "reader");
		var serializerExpr = Expression.Parameter(typeof(JsonSerializer), "serializer");
		var returnLabel = Expression.Label(typeof(DeserializeResult));
		var deserializeResultCtor = typeof(DeserializeResult).GetConstructors().First(x => x.GetParameters().Length == 2);

		var parameterlessCasesDeserializeExprs = unionCaseInfos
			.Where(x => x.Parameters.Length == 0)
			.Select(Expression (unionCase) =>
				Expression.Condition(
					Expression.Call(null, JsonConverterHelpers.CurrentTokenEqualsMethodInfo, readerExpr,
						Expression.Constant(unionCase.Name)),
					Expression.Return(
						returnLabel,
						Expression.New(
							deserializeResultCtor,
							Expression.Convert(Expression.Call(null, unionCase.CreateCaseMethod), typeof(IUnion)),
							Expression.Constant(false))),
					Expression.Empty()));

		var withParametersCasesDeserializeExprs = unionCaseInfos
			.Where(x => x.Parameters.Length > 0)
			.Select(unionCase =>
			{
				var deserializeExpression = GetDeserializeMultipleParametersCaseExpression(unionCase, unionType,
					readerExpr, serializerExpr);

				return Expression.Condition(
					Expression.Call(null, JsonConverterHelpers.CurrentTokenEqualsMethodInfo, readerExpr,
						Expression.Constant(unionCase.Name)),
					Expression.Block(
						Expression.Call(readerExpr, JsonConverterHelpers.ReadMethodInfo),
						Expression.Return(
							returnLabel,
							Expression.New(
								deserializeResultCtor,
								Expression.Convert(deserializeExpression, typeof(IUnion)),
								Expression.Constant(true)))),
					Expression.Empty());
			});

		var unionTypeExpr = Expression.Constant(unionType);
		var deserializeUnionBody = Expression.Block(
			Expression.Condition(
				Expression.Equal(
					Expression.Property(readerExpr, JsonConverterHelpers.TokenTypePropertyInfo),
					Expression.Constant(JsonToken.String)),
				Expression.Block([
					..parameterlessCasesDeserializeExprs,
					Expression.Call(
						null, JsonConverterHelpers.ThrowInvalidParameterlessCaseNameMethodInfo, readerExpr,
						unionTypeExpr)
				]),
				Expression.Block([
					Expression.Condition(
						Expression.Call(null, JsonConverterHelpers.ReadAndTokenIsPropertyNameMethodInfo,
							readerExpr),
						Expression.Empty(),
						Expression.Call(null, JsonConverterHelpers.ThrowInvalidUnionJsonObjectMethodInfo,
							readerExpr)),
					..withParametersCasesDeserializeExprs,
					Expression.Call(null, JsonConverterHelpers.ThrowInvalidCaseNameMethodInfo, readerExpr,
						unionTypeExpr),
				])),
			Expression.Label(returnLabel, Expression.Constant(default(DeserializeResult), typeof(DeserializeResult))));

		return Expression.Lambda<Func<JsonReader, JsonSerializer, DeserializeResult>>(deserializeUnionBody, readerExpr, serializerExpr).Compile();
	}

	private static Expression GetDeserializeMultipleParametersCaseExpression(
		UnionCaseInfo unionCase, Type unionType, ParameterExpression readerExpr, ParameterExpression serializerExpr)
	{
		var deserializedParametersCountExpr = Expression.Variable(typeof(int), "deserializedParametersCount");
		var breakLabel = Expression.Label("break");
		var continueLabel = Expression.Label("continue");

		var caseParameterExprs = unionCase.Parameters
			.Select(p => Expression.Variable(p.ParameterType, p.Name))
			.ToArray();

		var deserializeParameterExprs = unionCase.Parameters.Zip(caseParameterExprs, (x, y) => (x, y))
			.Select(Expression (x) =>
			{
				var (parameter, variableExpr) = x;
				var deserializeMethodInfo = JsonConverterHelpers.DeserializeGenericMethodInfo
					.MakeGenericMethod(parameter.ParameterType);

				return Expression.Condition(
					Expression.Call(null, JsonConverterHelpers.CurrentTokenEqualsMethodInfo, readerExpr,
						Expression.Constant(parameter.Name)),
					Expression.Block(
						Expression.Call(readerExpr, JsonConverterHelpers.ReadMethodInfo),
						Expression.Assign(
							variableExpr,
							Expression.Call(null, deserializeMethodInfo, readerExpr, serializerExpr)),
						Expression.PostIncrementAssign(deserializedParametersCountExpr),
						Expression.Continue(continueLabel)),
					Expression.Empty());
			})
			.ToArray();

		return Expression.Block(
			caseParameterExprs.Append(deserializedParametersCountExpr),
			Expression.Assign(deserializedParametersCountExpr, Expression.Constant(0)),
			Expression.Loop(
				Expression.Condition(
					Expression.Call(null, JsonConverterHelpers.ReadAndTokenIsPropertyNameMethodInfo, readerExpr),
					Expression.Block(deserializeParameterExprs),
					Expression.Break(breakLabel)),
				breakLabel, continueLabel),
			Expression.Condition(
				Expression.LessThan(deserializedParametersCountExpr, Expression.Constant(unionCase.Parameters.Length)),
				Expression.Call(null, JsonConverterHelpers.ThrowNotAllCaseParametersPresentMethodInfo,
					Expression.Constant(unionType), Expression.Constant(unionCase.Name),
					deserializedParametersCountExpr, Expression.Constant(unionCase.Parameters.Length)),
				Expression.Empty()),
			Expression.Call(null, unionCase.CreateCaseMethod, caseParameterExprs));
	}

	private readonly struct UnionCaseParameterInfo
	{
		public string Name { get; }

		public Type ParameterType { get; }

		public UnionCaseParameterInfo(string name, Type parameterType)
		{
			Name = name;
			ParameterType = parameterType;
		}
	}

	private sealed class UnionCaseInfo
	{
		public string Name { get; }

		public MethodInfo CreateCaseMethod { get; }

		public MethodInfo TryGetDataMethod { get; }

		public UnionCaseParameterInfo[] Parameters { get; }

		public UnionCaseInfo(MethodInfo createCaseMethod)
		{
			Name = createCaseMethod.Name;
			CreateCaseMethod = createCaseMethod;
			TryGetDataMethod = createCaseMethod.DeclaringType!.GetMethod(
				UnionNamesProvider.GetTryGetCaseDataMethodName(Name), BindingFlags.Public | BindingFlags.Instance)!;
			Parameters = createCaseMethod.GetParameters()
				.Select(p => new UnionCaseParameterInfo(p.Name!, p.ParameterType))
				.ToArray();
		}
	}

	private readonly struct DeserializeResult
	{
		public IUnion Union { get; }

		public bool HasParameters { get; }

		public DeserializeResult(IUnion union, bool hasParameters)
		{
			Union = union;
			HasParameters = hasParameters;
		}
	}

	private sealed class UnionConverter
	{
		public Type UnionType { get; }

		public Action<JsonWriter, IUnion, JsonSerializer> Serializer { get; }

		public Func<JsonReader, JsonSerializer, DeserializeResult> Deserializer { get; }

		public UnionConverter(
			Type unionType, Action<JsonWriter, IUnion, JsonSerializer> serializer,
			Func<JsonReader, JsonSerializer, DeserializeResult> deserializer)
		{
			UnionType = unionType;
			Serializer = serializer;
			Deserializer = deserializer;
		}
	}
}