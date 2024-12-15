using System;
using System.Collections.Generic;
using System.Linq;
using Dusharp.CodeAnalyzing;
using Dusharp.CodeGeneration;
using Dusharp.Extensions;
using Dusharp.SourceGenerator.Common;
using Microsoft.CodeAnalysis;

namespace Dusharp.UnionGeneration;

public sealed class UnionCodeGenerator
{
	private static readonly string ThrowIfNullMethod =
		$"{typeof(ExceptionUtils).FullName}.{nameof(ExceptionUtils.ThrowIfNull)}";

	private readonly TypeCodeWriter _typeCodeWriter;
	private readonly IUnionDefinitionGeneratorFactory _unionDefinitionGeneratorFactory;

	public UnionCodeGenerator(
		TypeCodeWriter typeCodeWriter, IUnionDefinitionGeneratorFactory unionDefinitionGeneratorFactory)
	{
		_typeCodeWriter = typeCodeWriter;
		_unionDefinitionGeneratorFactory = unionDefinitionGeneratorFactory;
	}

	public string? GenerateUnion(UnionInfo unionInfo)
	{
		return unionInfo.Cases.Count == 0 ? null : GenerateUnionImpl(unionInfo);
	}

	private string GenerateUnionImpl(UnionInfo unionInfo)
	{
		using var codeWriter = new CodeWriter();
		codeWriter.AppendLine("#nullable enable");

		CodeWritingUtils.WriteOuterBlocks(
			unionInfo.TypeSymbol, codeWriter,
			innerBlock =>
			{
				innerBlock.WriteSuppressWarning("CA1000", "For generic unions.");
				var unionDefinitionGenerator = _unionDefinitionGeneratorFactory.Create(unionInfo);
				var unionTypeDefinition =
					new UnionTypeDefinitionGenerator(unionDefinitionGenerator, unionInfo).Generate();
				_typeCodeWriter.WriteType(unionTypeDefinition, innerBlock);

				foreach (var typeDefinition in unionDefinitionGenerator.GetAdditionalTypes())
				{
					_typeCodeWriter.WriteType(typeDefinition, innerBlock);
				}
			});

		return codeWriter.ToString();
	}

	private sealed class UnionTypeDefinitionGenerator
	{
		private readonly IUnionDefinitionGenerator _unionDefinitionGenerator;
		private readonly UnionInfo _union;
		private readonly TypeName _unionTypeName;
		private readonly string _nullableUnionTypeName;

		public UnionTypeDefinitionGenerator(IUnionDefinitionGenerator unionDefinitionGenerator, UnionInfo union)
		{
			_unionDefinitionGenerator = unionDefinitionGenerator;
			_union = union;
			_unionTypeName = union.GetTypeName();
			_nullableUnionTypeName = _union.TypeSymbol.IsValueType
				? _unionTypeName.FullName
				: $"{_unionTypeName.FullName}?";
		}

		public TypeDefinition Generate()
		{
			var unionTypeDefinition = new TypeDefinition
			{
				Name = _union.Name,
				Kind = _unionDefinitionGenerator.TypeKind,
				IsPartial = true,
				GenericParameters = _union.TypeSymbol.TypeParameters.Select(x => x.Name).ToArray(),
				InheritedTypes = [$"System.IEquatable<{_unionTypeName.FullName}>", "global::Dusharp.IUnion"],
				Properties = _union.Cases.Select(GetIsCaseProperty).ToArray(),
				Methods = _union.Cases
					.Select(GetUnionCaseMethod)
					.Concat(
					[
						GetMatchMethod(MatchMethodConfiguration.WithoutStateWithoutReturn),
						GetMatchMethod(MatchMethodConfiguration.WithoutStateWithReturn),
						GetMatchMethod(MatchMethodConfiguration.WithStateWithoutReturn),
						GetMatchMethod(MatchMethodConfiguration.WithStateWithReturn),
					])
					.Concat(_union.Cases.Select(GetTryGetCaseDataMethod))
					.Concat(GetDefaultEqualsMethods())
					.ToArray(),
				Operators = GetEqualityOperators(_nullableUnionTypeName, _unionDefinitionGenerator),
			};

			return _unionDefinitionGenerator.AdjustUnionTypeDefinition(unionTypeDefinition);
		}

		private PropertyDefinition GetIsCaseProperty(UnionCaseInfo unionCase) =>
			new()
			{
				Name = UnionNamesProvider.GetIsCasePropertyName(unionCase.Name),
				Accessibility = Accessibility.Public,
				TypeName = "bool",
				Getter = new PropertyDefinition.PropertyAccessor(
					null,
					PropertyDefinition.PropertyAccessorImpl.Bodied((_, bodyBlock) =>
						bodyBlock.AppendLine($"return {_unionDefinitionGenerator.GetUnionCaseCheckExpression(unionCase)};"))),
			};

		private MethodDefinition GetUnionCaseMethod(UnionCaseInfo unionCase) =>
			new()
			{
				Name = unionCase.Name,
				ReturnType = _unionTypeName.FullName,
				Accessibility = Accessibility.Public,
				MethodModifier = MethodModifier.Static(),
				IsPartial = true,
				Parameters = unionCase.Parameters.Select(x => new MethodParameter(x.TypeName, x.Name)).ToArray(),
				BodyWriter = _unionDefinitionGenerator.GetUnionCaseMethodBodyWriter(unionCase),
			};

		private MethodDefinition GetMatchMethod(MatchMethodConfiguration methodConfiguration)
		{
			var matchDelegateParameters = _union.Cases
				.Select(x => new MethodParameter(
					methodConfiguration.MatchCaseDelegateTypeProvider(string.Join(", ", x.Parameters.Select(y => y.TypeName))),
					$"{char.ToLowerInvariant(x.Name[0])}{x.Name.AsSpan(1).ToString()}Case"))
				.ToArray();

			return new MethodDefinition
			{
				Name = "Match",
				ReturnType = methodConfiguration.ReturnType,
				Accessibility = Accessibility.Public,
				Parameters = methodConfiguration.MethodParametersExtender(matchDelegateParameters).ToArray(),
				GenericParameters = methodConfiguration.GenericParameters,
				BodyWriter = (_, methodBlock) =>
				{
					foreach (var matchDelegateParameter in matchDelegateParameters)
					{
						methodBlock.AppendLine(
							$"{ThrowIfNullMethod}({matchDelegateParameter.Name}, \"{matchDelegateParameter.Name}\");");
					}

					methodBlock.AppendLine();
					foreach (var (unionCase, parameterName) in _union.Cases.Zip(matchDelegateParameters, (x, y) => (x, y.Name)))
					{
						methodBlock.AppendLine($"if ({UnionNamesProvider.GetIsCasePropertyName(unionCase.Name)})");
						using var thenBlock = methodBlock.NewBlock();
						var argumentsStr = string.Join(
							", ", _unionDefinitionGenerator.GetUnionCaseParameterAccessors(unionCase));
						thenBlock.AppendLine(methodConfiguration.MatchBodyProvider(parameterName, argumentsStr));
					}

					methodBlock.AppendLine(UnionGenerationUtils.ThrowUnionInInvalidStateCode);
					if (methodConfiguration.ReturnType != "void")
					{
						methodBlock.AppendLine("return default!;");
					}
				},
			};
		}

		private MethodDefinition GetTryGetCaseDataMethod(UnionCaseInfo unionCase) =>
			new()
			{
				Name = UnionNamesProvider.GetTryGetCaseDataMethodName(unionCase.Name),
				ReturnType = "bool",
				Accessibility = Accessibility.Public,
				Parameters = unionCase.Parameters
					.Select(x => new MethodParameter(x.TypeName, x.Name, MethodParameterModifier.Out()))
					.ToArray(),
				BodyWriter = (_, methodBodyBlock) =>
				{
					methodBodyBlock.AppendLine($"if ({UnionNamesProvider.GetIsCasePropertyName(unionCase.Name)})");
					using (var thenBlock = methodBodyBlock.NewBlock())
					{
						foreach (var (parameter, accessor) in unionCase.Parameters
							         .Zip(_unionDefinitionGenerator.GetUnionCaseParameterAccessors(unionCase), (x, y) => (x, y)))
						{
							thenBlock.AppendLine($"{parameter.Name} = {accessor};");
						}

						thenBlock.AppendLine("return true;");
					}

					foreach (var parameter in unionCase.Parameters)
					{
						methodBodyBlock.AppendLine($"{parameter.Name} = default({parameter.TypeName})!;");
					}

					methodBodyBlock.AppendLine("return false;");
				},
			};

		private MethodDefinition[] GetDefaultEqualsMethods() =>
		[
			_unionDefinitionGenerator.AdjustSpecificEqualsMethod(new MethodDefinition
			{
				Name = "Equals",
				Accessibility = Accessibility.Public,
				ReturnType = "bool",
				Parameters = [new MethodParameter(_nullableUnionTypeName, "other")],
			}),
			_unionDefinitionGenerator.AdjustDefaultEqualsMethod(new MethodDefinition
			{
				Name = "Equals",
				Accessibility = Accessibility.Public,
				ReturnType = "bool",
				Parameters = [new MethodParameter("object?", "other")],
				MethodModifier = MethodModifier.Override(),
			}),
			new MethodDefinition
			{
				Name = "GetHashCode",
				Accessibility = Accessibility.Public,
				ReturnType = "int",
				MethodModifier = MethodModifier.Override(),
				BodyWriter = _unionDefinitionGenerator.GetGetHashCodeMethodBodyWriter(),
			},
		];

		private static OperatorDefinition[] GetEqualityOperators(
			string nullableUnionTypeName, IUnionDefinitionGenerator unionDefinitionGenerator) =>
		[
			new OperatorDefinition
			{
				Name = "==",
				ReturnType = "bool",
				Parameters =
				[
					new MethodParameter(nullableUnionTypeName, "left"),
					new MethodParameter(nullableUnionTypeName, "right"),
				],
				BodyWriter = unionDefinitionGenerator.GetEqualityOperatorBodyWriter(),
			},
			new OperatorDefinition
			{
				Name = "!=",
				ReturnType = "bool",
				Parameters =
				[
					new MethodParameter(nullableUnionTypeName, "left"),
					new MethodParameter(nullableUnionTypeName, "right"),
				],
				BodyWriter = static (_, operatorBodyBlock) => operatorBodyBlock.AppendLine("return !(left == right);"),
			},
		];
	}

	private sealed class MatchMethodConfiguration
	{
		public static readonly MatchMethodConfiguration WithoutStateWithoutReturn = new(
			"void",
			caseTypeParameters => string.IsNullOrEmpty(caseTypeParameters)
				? "System.Action"
				: $"System.Action<{caseTypeParameters}>",
			(matchHandlerParameterName, caseParameters) => $"{matchHandlerParameterName}({caseParameters}); return;");

		public static readonly MatchMethodConfiguration WithoutStateWithReturn = new(
			"TRet",
			caseTypeParameters => string.IsNullOrEmpty(caseTypeParameters)
				? "System.Func<TRet>"
				: $"System.Func<{caseTypeParameters}, TRet>",
			(matchHandlerParameterName, caseParameters) => $"return {matchHandlerParameterName}({caseParameters});")
		{
			GenericParameters = ["TRet"],
		};

		public static readonly MatchMethodConfiguration WithStateWithoutReturn = new(
			"void",
			caseTypeParameters => string.IsNullOrEmpty(caseTypeParameters)
				? "System.Action<TState>"
				: $"System.Action<TState, {caseTypeParameters}>",
			(matchHandlerParameterName, caseParameters) =>
			{
				caseParameters = string.IsNullOrEmpty(caseParameters) ? string.Empty : $", {caseParameters}";
				return $"{matchHandlerParameterName}(state{caseParameters}); return;";
			})
		{
			GenericParameters = ["TState"],
			MethodParametersExtender =
				methodParameters => methodParameters.Prepend(new MethodParameter("TState", "state")),
		};

		public static readonly MatchMethodConfiguration WithStateWithReturn = new(
			"TRet",
			caseTypeParameters => string.IsNullOrEmpty(caseTypeParameters)
				? "System.Func<TState, TRet>"
				: $"System.Func<TState, {caseTypeParameters}, TRet>",
			(matchHandlerParameterName, caseParameters) =>
			{
				caseParameters = string.IsNullOrEmpty(caseParameters) ? string.Empty : $", {caseParameters}";
				return $"return {matchHandlerParameterName}(state{caseParameters});";
			})
		{
			GenericParameters = ["TState", "TRet"],
			MethodParametersExtender =
				methodParameters => methodParameters.Prepend(new MethodParameter("TState", "state")),
		};

		public string ReturnType { get; }

		public IReadOnlyList<string> GenericParameters { get; init; } = [];

		public Func<string, string> MatchCaseDelegateTypeProvider { get; }

		public Func<string, string, string> MatchBodyProvider { get; }

		public Func<IEnumerable<MethodParameter>, IEnumerable<MethodParameter>> MethodParametersExtender { get; private init; } =
			x => x;

		public MatchMethodConfiguration(string returnType, Func<string, string> matchCaseDelegateTypeProvider,
			Func<string, string, string> matchBodyProvider)
		{
			ReturnType = returnType;
			MatchCaseDelegateTypeProvider = matchCaseDelegateTypeProvider;
			MatchBodyProvider = matchBodyProvider;
		}
	}
}