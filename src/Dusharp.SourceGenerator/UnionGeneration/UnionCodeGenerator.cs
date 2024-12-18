using System;
using System.Collections.Generic;
using System.Linq;
using Dusharp.CodeAnalyzing;
using Dusharp.CodeGeneration;
using Dusharp.SourceGenerator.Common;
using Microsoft.CodeAnalysis;
using TypeInfo = Dusharp.CodeAnalyzing.TypeInfo;
using TypeName = Dusharp.CodeAnalyzing.TypeName;

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

	public string GenerateUnion(UnionInfo unionInfo)
	{
		using var codeWriter = CodeWriter.CreateWithDefaultLines();

		CodeWritingUtils.WriteContainingBlocks(
			unionInfo.TypeInfo, codeWriter,
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
		private readonly TypeName _nullableUnionTypeName;

		public UnionTypeDefinitionGenerator(IUnionDefinitionGenerator unionDefinitionGenerator, UnionInfo union)
		{
			_unionDefinitionGenerator = unionDefinitionGenerator;
			_union = union;
			_nullableUnionTypeName = new TypeName(_union.TypeInfo, true);
		}

		public TypeDefinition Generate()
		{
			var unionTypeDefinition = new TypeDefinition
			{
				Name = _union.TypeInfo.Name,
				Kind = _unionDefinitionGenerator.TypeKind,
				IsPartial = true,
				InheritedTypes =
				[
					TypeInfos.IEquatable(new TypeName(_union.TypeInfo, false)),
					TypeInfos.IUnion,
				],
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
				TypeName = TypeNames.Boolean,
				Getter = new PropertyDefinition.PropertyAccessor(
					null,
					PropertyDefinition.PropertyAccessorImpl.Bodied((_, bodyBlock) =>
						bodyBlock.AppendLine($"return {_unionDefinitionGenerator.GetUnionCaseCheckExpression(unionCase)};"))),
			};

		private MethodDefinition GetUnionCaseMethod(UnionCaseInfo unionCase) =>
			new()
			{
				Name = unionCase.Name,
				ReturnType = new TypeName(_union.TypeInfo, false),
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
					methodConfiguration.MatchCaseDelegateTypeProvider(x.Parameters.Select(y => y.TypeName).ToArray()),
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
					if (methodConfiguration.ReturnType != TypeNames.Void)
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
				ReturnType = TypeNames.Boolean,
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
				ReturnType = TypeNames.Boolean,
				Parameters = [new MethodParameter(_nullableUnionTypeName, "other")],
			}),
			_unionDefinitionGenerator.AdjustDefaultEqualsMethod(new MethodDefinition
			{
				Name = "Equals",
				Accessibility = Accessibility.Public,
				ReturnType = TypeNames.Boolean,
				Parameters = [new MethodParameter(TypeNames.Object(true), "other")],
				MethodModifier = MethodModifier.Override(),
			}),
			new MethodDefinition
			{
				Name = "GetHashCode",
				Accessibility = Accessibility.Public,
				ReturnType = TypeNames.Int32,
				MethodModifier = MethodModifier.Override(),
				BodyWriter = _unionDefinitionGenerator.GetGetHashCodeMethodBodyWriter(),
			},
		];

		private static OperatorDefinition[] GetEqualityOperators(
			TypeName nullableUnionTypeName, IUnionDefinitionGenerator unionDefinitionGenerator) =>
		[
			new OperatorDefinition
			{
				Name = "==",
				ReturnType = TypeNames.Boolean,
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
				ReturnType = TypeNames.Boolean,
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
		private static readonly TypeName TStateName =
			new(TypeInfo.SpecialName("TState", TypeInfo.TypeKind.Unknown()), false);

		private static readonly TypeName TRetName =
			new(TypeInfo.SpecialName("TRet", TypeInfo.TypeKind.Unknown()), false);

		public static readonly MatchMethodConfiguration WithoutStateWithoutReturn = new(
			TypeNames.Void,
			caseTypeParameters => new TypeName(TypeInfos.Action(caseTypeParameters), false),
			(matchHandlerParameterName, caseParameters) => $"{matchHandlerParameterName}({caseParameters}); return;");

		public static readonly MatchMethodConfiguration WithoutStateWithReturn = new(
			TRetName,
			caseTypeParameters => new TypeName(TypeInfos.Func(caseTypeParameters, TRetName), false),
			(matchHandlerParameterName, caseParameters) => $"return {matchHandlerParameterName}({caseParameters});")
		{
			GenericParameters = [TRetName.Name],
		};

		public static readonly MatchMethodConfiguration WithStateWithoutReturn = new(
			TypeNames.Void,
			caseTypeParameters => new TypeName(TypeInfos.Action([TStateName, ..caseTypeParameters]), false),
			(matchHandlerParameterName, caseParameters) =>
			{
				caseParameters = string.IsNullOrEmpty(caseParameters) ? string.Empty : $", {caseParameters}";
				return $"{matchHandlerParameterName}(state{caseParameters}); return;";
			})
		{
			GenericParameters = [TStateName.Name],
			MethodParametersExtender =
				methodParameters => methodParameters.Prepend(new MethodParameter(TStateName, "state")),
		};

		public static readonly MatchMethodConfiguration WithStateWithReturn = new(
			TRetName,
			caseTypeParameters => new TypeName(TypeInfos.Func([TStateName, ..caseTypeParameters], TRetName), false),
			(matchHandlerParameterName, caseParameters) =>
			{
				caseParameters = string.IsNullOrEmpty(caseParameters) ? string.Empty : $", {caseParameters}";
				return $"return {matchHandlerParameterName}(state{caseParameters});";
			})
		{
			GenericParameters = [TStateName.Name, TRetName.Name],
			MethodParametersExtender =
				methodParameters => methodParameters.Prepend(new MethodParameter(TStateName, "state")),
		};

		public TypeName ReturnType { get; }

		public IReadOnlyList<string> GenericParameters { get; init; } = [];

		public Func<IReadOnlyCollection<TypeName>, TypeName> MatchCaseDelegateTypeProvider { get; }

		public Func<string, string, string> MatchBodyProvider { get; }

		public Func<IEnumerable<MethodParameter>, IEnumerable<MethodParameter>> MethodParametersExtender { get; private init; } =
			x => x;

		public MatchMethodConfiguration(TypeName returnType, Func<IReadOnlyCollection<TypeName>, TypeName> matchCaseDelegateTypeProvider,
			Func<string, string, string> matchBodyProvider)
		{
			ReturnType = returnType;
			MatchCaseDelegateTypeProvider = matchCaseDelegateTypeProvider;
			MatchBodyProvider = matchBodyProvider;
		}
	}
}