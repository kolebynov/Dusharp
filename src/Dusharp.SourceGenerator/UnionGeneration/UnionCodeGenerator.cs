using System;
using System.Collections.Generic;
using System.Linq;
using Dusharp.CodeAnalyzing;
using Dusharp.CodeGeneration;
using Microsoft.CodeAnalysis;

namespace Dusharp.UnionGeneration;

public sealed class UnionCodeGenerator
{
	private static readonly string ThrowIfNullMethod =
		$"{typeof(ExceptionUtils).FullName}.{nameof(ExceptionUtils.ThrowIfNull)}";
	private static readonly string ThrowUnionInInvalidStateMethod =
		$"{typeof(ExceptionUtils).FullName}.{nameof(ExceptionUtils.ThrowUnionInInvalidState)}";

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
				var unionTypeDefinition = new TypeDefinition
				{
					Name = unionInfo.Name,
					Kind = unionDefinitionGenerator.TypeKind,
					IsPartial = true,
					GenericParameters = unionInfo.TypeSymbol.TypeParameters
						.Select(x => x.Name)
						.ToArray(),
				};

				var nullableUnionTypeName = unionInfo.TypeSymbol.IsValueType
					? unionTypeDefinition.FullName
					: $"{unionTypeDefinition.FullName}?";
				unionTypeDefinition = unionTypeDefinition with
				{
					InheritedTypes = [$"System.IEquatable<{unionTypeDefinition.FullName}>"],
					Methods =
					[
						.. unionInfo.Cases.Select(x => GetUnionCaseMethod(x, unionTypeDefinition.FullName, unionDefinitionGenerator)),
						GetMatchMethod(unionInfo, MatchMethodConfiguration.WithoutStateWithoutReturn, unionTypeDefinition.FullName, unionDefinitionGenerator),
						GetMatchMethod(unionInfo, MatchMethodConfiguration.WithoutStateWithReturn, unionTypeDefinition.FullName, unionDefinitionGenerator),
						GetMatchMethod(unionInfo, MatchMethodConfiguration.WithStateWithoutReturn, unionTypeDefinition.FullName, unionDefinitionGenerator),
						GetMatchMethod(unionInfo, MatchMethodConfiguration.WithStateWithReturn, unionTypeDefinition.FullName, unionDefinitionGenerator),
						.. GetDefaultEqualsMethods(nullableUnionTypeName, unionDefinitionGenerator),
					],
					Operators = GetEqualityOperators(nullableUnionTypeName, unionDefinitionGenerator),
				};

				unionTypeDefinition = unionDefinitionGenerator.AddAdditionalInfo(unionTypeDefinition);

				_typeCodeWriter.WriteType(unionTypeDefinition, innerBlock);
			});

		return codeWriter.ToString();
	}

	private static MethodDefinition GetUnionCaseMethod(UnionCaseInfo unionCase, string unionTypeName,
		IUnionDefinitionGenerator unionDefinitionGenerator) =>
		new MethodDefinition
		{
			Name = unionCase.Name,
			ReturnType = unionTypeName,
			Accessibility = Accessibility.Public,
			MethodModifier = MethodModifier.Static(),
			IsPartial = true,
			Parameters = unionCase.Parameters.Select(x => new MethodParameter(x.TypeName, x.Name)).ToArray(),
			BodyWriter = unionDefinitionGenerator.GetUnionCaseMethodBodyWriter(unionCase, unionTypeName),
		};

	private static MethodDefinition GetMatchMethod(
		UnionInfo union, MatchMethodConfiguration methodConfiguration, string unionTypeName,
		IUnionDefinitionGenerator unionDefinitionGenerator)
	{
		var matchDelegateParameters = union.Cases
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
					methodBlock.AppendLine($"{ThrowIfNullMethod}({matchDelegateParameter.Name}, \"{matchDelegateParameter.Name}\");");
				}

				methodBlock.AppendLine();
				foreach (var (unionCase, parameterName) in union.Cases.Zip(matchDelegateParameters, (x, y) => (x, y.Name)))
				{
					using var matchBlock = methodBlock.NewBlock();
					unionDefinitionGenerator.WriteMatchBlock(
						unionCase, argumentsStr => methodConfiguration.MatchBodyProvider(parameterName, argumentsStr),
						matchBlock, unionTypeName);
				}

				methodBlock.AppendLine($"{ThrowUnionInInvalidStateMethod}();");
				if (methodConfiguration.ReturnType != "void")
				{
					methodBlock.AppendLine("return default!;");
				}
			},
		};
	}

	private static MethodDefinition[] GetDefaultEqualsMethods(string nullableUnionTypeName, IUnionDefinitionGenerator unionDefinitionGenerator) =>
	[
		unionDefinitionGenerator.AdjustSpecificEqualsMethod(new MethodDefinition
		{
			Name = "Equals",
			Accessibility = Accessibility.Public,
			ReturnType = "bool",
			Parameters = [new MethodParameter(nullableUnionTypeName, "other")],
		}),
		unionDefinitionGenerator.AdjustDefaultEqualsMethod(new MethodDefinition
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
			BodyWriter = unionDefinitionGenerator.GetGetHashCodeMethodBodyWriter(),
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
			MethodParametersExtender = methodParameters => methodParameters.Prepend(new MethodParameter("TState", "state")),
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
			MethodParametersExtender = methodParameters => methodParameters.Prepend(new MethodParameter("TState", "state")),
		};

		public string ReturnType { get; }

		public IReadOnlyList<string> GenericParameters { get; init; } = [];

		public Func<string, string> MatchCaseDelegateTypeProvider { get; }

		public Func<string, string, string> MatchBodyProvider { get; }

		public Func<IEnumerable<MethodParameter>, IEnumerable<MethodParameter>> MethodParametersExtender { get; private init; } = x => x;

		public MatchMethodConfiguration(string returnType, Func<string, string> matchCaseDelegateTypeProvider,
			Func<string, string, string> matchBodyProvider)
		{
			ReturnType = returnType;
			MatchCaseDelegateTypeProvider = matchCaseDelegateTypeProvider;
			MatchBodyProvider = matchBodyProvider;
		}
	}
}