using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dusharp.SourceGenerator.Common;

namespace Dusharp.Json;

public sealed class DefaultUnionJsonConverter : JsonConverter<IUnion>
{
	private readonly ConcurrentDictionary<Type, UnionConverter> _converters = new();

	public override bool CanConvert(Type typeToConvert) =>
		typeof(IUnion).IsAssignableFrom(typeToConvert) && TryGetConverter(typeToConvert) != null;

	public override IUnion? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var unionConverter = GetConverter(typeToConvert);
		JsonConverterHelpers.BeforeRead(ref reader, unionConverter.UnionType);
		var value = unionConverter.Deserializer(ref reader, options);
		JsonConverterHelpers.AfterRead(ref reader, unionConverter.UnionType);

		return value;
	}

	public override void Write(Utf8JsonWriter writer, IUnion value, JsonSerializerOptions options)
	{
		GetConverter(value.GetType()).Serializer(writer, value, options);
	}

	private UnionConverter GetConverter(Type unionType) =>
		TryGetConverter(unionType) ?? throw new ArgumentException($"{unionType} is not a valid union type.");

	private UnionConverter? TryGetConverter(Type unionType)
	{
		var realUnionType = GetRealUnionType(unionType);
		return realUnionType != null ? _converters.GetOrAdd(realUnionType, static t => CreateConverter(t)) : null;
	}

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

	private static Action<Utf8JsonWriter, IUnion, JsonSerializerOptions> CreateSerializer(
		Type unionType, IReadOnlyCollection<UnionCaseInfo> unionCaseInfos)
	{
		var writerExpr = Expression.Parameter(typeof(Utf8JsonWriter), "writer");
		var valueExpr = Expression.Parameter(typeof(IUnion), "value");
		var jsonOptionsExpr = Expression.Parameter(typeof(JsonSerializerOptions), "options");
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
					0 => GetParameterlessCaseSerializeExpression(unionCase.EncodedName.EncodedValue, writerExpr),
					_ => GetMultipleParametersCaseSerializeExpression(unionCase, caseParameterVariableExprs, writerExpr,
						jsonOptionsExpr),
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
			Expression.Call(null, JsonConverterHelpers.WriteEmptyObjectMethodInfo, writerExpr),
			Expression.Label(returnLabel),
		]);

		return Expression
			.Lambda<Action<Utf8JsonWriter, IUnion, JsonSerializerOptions>>(serializerBody, writerExpr, valueExpr, jsonOptionsExpr)
			.Compile();
	}

	private static Expression GetParameterlessCaseSerializeExpression(
		JsonEncodedText unionCaseName, ParameterExpression writerExpr) =>
		Expression.Call(writerExpr, JsonConverterHelpers.WriteStringValueMethodInfo, Expression.Constant(unionCaseName));

	private static Expression GetMultipleParametersCaseSerializeExpression(
		UnionCaseInfo unionCase, ParameterExpression[] caseParameterVariableExprs, ParameterExpression writerExpr,
		ParameterExpression jsonOptionsExpr) =>
		Expression.Block([
			Expression.Call(writerExpr, JsonConverterHelpers.WriteStartObjectMethodInfo),
			Expression.Call(writerExpr, JsonConverterHelpers.WriteStartObjectWithPropertyMethodInfo,
				Expression.Constant(unionCase.EncodedName.EncodedValue)),
			..unionCase.Parameters
				.Zip(caseParameterVariableExprs, (x, y) => (x, y))
				.Select(Expression (x) =>
				{
					var (parameter, variable) = x;
					var writePropertyMethodInfo = JsonConverterHelpers.WritePropertyGenericMethodInfo
						.MakeGenericMethod(parameter.ParameterType);

					return Expression.Call(null, writePropertyMethodInfo, writerExpr,
						Expression.Constant(parameter.EncodedName.EncodedValue), variable, jsonOptionsExpr);
				}),
			Expression.Call(writerExpr, JsonConverterHelpers.WriteEndObjectMethodInfo),
			Expression.Call(writerExpr, JsonConverterHelpers.WriteEndObjectMethodInfo),
		]);

	private static DeserializeCaseDelegate CreateDeserializer(
		Type unionType, IReadOnlyCollection<UnionCaseInfo> unionCaseInfos)
	{
		var readerExpr = Expression.Parameter(typeof(Utf8JsonReader).MakeByRefType(), "reader");
		var jsonOptionsExpr = Expression.Parameter(typeof(JsonSerializerOptions), "options");
		var returnLabel = Expression.Label(typeof(IUnion));

		var parameterlessCasesDeserializeExprs = unionCaseInfos
			.Where(x => x.Parameters.Length == 0)
			.Select(Expression (unionCase) =>
				Expression.Condition(
					Expression.Call(null, JsonConverterHelpers.ValueTextEqualsMethodInfo, readerExpr,
						Expression.Constant(unionCase.EncodedName.Utf8Value)),
					Expression.Return(
						returnLabel,
						Expression.Convert(Expression.Call(null, unionCase.CreateCaseMethod), typeof(IUnion))),
					Expression.Empty()));

		var withParametersCasesDeserializeExprs = unionCaseInfos
			.Where(x => x.Parameters.Length > 0)
			.Select(unionCase =>
			{
				var deserializeExpression = GetDeserializeMultipleParametersCaseExpression(unionCase, unionType,
					readerExpr, jsonOptionsExpr);

				return Expression.Condition(
					Expression.Call(null, JsonConverterHelpers.ValueTextEqualsMethodInfo, readerExpr,
						Expression.Constant(unionCase.EncodedName.Utf8Value)),
					Expression.Block(
						Expression.Call(readerExpr, JsonConverterHelpers.ReadMethodInfo),
						Expression.Return(returnLabel, Expression.Convert(deserializeExpression, typeof(IUnion)))),
					Expression.Empty());
			});

		var unionTypeExpr = Expression.Constant(unionType);
		var deserializeUnionBody = Expression.Block(
			Expression.Condition(
				Expression.Equal(
					Expression.Property(readerExpr, JsonConverterHelpers.TokenTypePropertyInfo),
					Expression.Constant(JsonTokenType.String)),
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
			Expression.Label(returnLabel, Expression.Constant(null, typeof(IUnion))));

		return Expression.Lambda<DeserializeCaseDelegate>(deserializeUnionBody, readerExpr, jsonOptionsExpr).Compile();
	}

	private static Expression GetDeserializeMultipleParametersCaseExpression(
		UnionCaseInfo unionCase, Type unionType, ParameterExpression readerExpr, ParameterExpression jsonOptionsExpr)
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
					Expression.Call(null, JsonConverterHelpers.ValueTextEqualsMethodInfo, readerExpr,
						Expression.Constant(parameter.EncodedName.Utf8Value)),
					Expression.Block(
						Expression.Assign(
							variableExpr,
							Expression.Call(null, deserializeMethodInfo, readerExpr, jsonOptionsExpr)),
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

	private static Type? GetRealUnionType(Type unionType)
	{
		if (unionType.IsValueType)
		{
			return unionType.GetCustomAttribute<UnionAttribute>(false) is null ? null : unionType;
		}

		return FlattenHierarchy(unionType).FirstOrDefault(x => x.GetCustomAttribute<UnionAttribute>(false) is not null);
	}

	private static IEnumerable<Type> FlattenHierarchy(Type type)
	{
		var currentType = type;
		while (currentType != null)
		{
			yield return currentType;
			currentType = currentType.BaseType;
		}
	}

	private readonly struct UnionCaseParameterInfo
	{
		public string Name { get; }

		public JsonEncodedValue EncodedName { get; }

		public Type ParameterType { get; }

		public UnionCaseParameterInfo(string name, Type parameterType)
		{
			Name = name;
			ParameterType = parameterType;
			EncodedName = new JsonEncodedValue(name);
		}
	}

	private sealed class UnionCaseInfo
	{
		public string Name { get; }

		public JsonEncodedValue EncodedName { get; }

		public MethodInfo CreateCaseMethod { get; }

		public MethodInfo TryGetDataMethod { get; }

		public UnionCaseParameterInfo[] Parameters { get; }

		public UnionCaseInfo(MethodInfo createCaseMethod)
		{
			Name = createCaseMethod.Name;
			EncodedName = new JsonEncodedValue(Name);
			CreateCaseMethod = createCaseMethod;
			TryGetDataMethod = createCaseMethod.DeclaringType!.GetMethod(
				UnionNamesProvider.GetTryGetCaseDataMethodName(Name), BindingFlags.Public | BindingFlags.Instance)!;
			Parameters = createCaseMethod.GetParameters()
				.Select(p => new UnionCaseParameterInfo(p.Name, p.ParameterType))
				.ToArray();
		}
	}

	private delegate IUnion DeserializeCaseDelegate(ref Utf8JsonReader reader, JsonSerializerOptions options);

	private sealed class UnionConverter
	{
		public Type UnionType { get; }

		public Action<Utf8JsonWriter, IUnion, JsonSerializerOptions> Serializer { get; }

		public DeserializeCaseDelegate Deserializer { get; }

		public UnionConverter(
			Type unionType, Action<Utf8JsonWriter, IUnion, JsonSerializerOptions> serializer,
			DeserializeCaseDelegate deserializer)
		{
			UnionType = unionType;
			Serializer = serializer;
			Deserializer = deserializer;
		}
	}
}