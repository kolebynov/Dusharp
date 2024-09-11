using System;
using System.Linq;

namespace Dusharp;

public static class UnionCodeGenerator
{
	private static readonly string ThrowIfNullMethod =
		$"{typeof(ExceptionUtils).FullName}.{nameof(ExceptionUtils.ThrowIfNull)}";
	private static readonly string ThrowUnionInInvalidStateMethod =
		$"{typeof(ExceptionUtils).FullName}.{nameof(ExceptionUtils.ThrowUnionInInvalidState)}";

	public static string? GenerateClassUnion(UnionInfo unionInfo)
	{
		if (unionInfo.Cases.Count == 0)
		{
			return null;
		}

		using var codeWriter = CodeWritingUtils.WriteOuterBlocks(
			unionInfo.TypeSymbol,
			innerBlock =>
			{
				innerBlock.AppendLine($"partial class {unionInfo.Name}");
				using var unionBodyBlock = innerBlock.NewBlock();
				unionBodyBlock.AppendLine($"private {unionInfo.Name}() {{}}");
				unionBodyBlock.AppendLine();

				foreach (var caseMethodSymbol in unionInfo.Cases)
				{
					WriteUnionCase(caseMethodSymbol, unionInfo, unionBodyBlock);
				}

				WriteMatchMethod(unionInfo, unionBodyBlock, MatchMethodBuilder.WithoutStateWithoutReturn);
				WriteMatchMethod(unionInfo, unionBodyBlock, MatchMethodBuilder.WithoutStateWithReturn);
				WriteMatchMethod(unionInfo, unionBodyBlock, MatchMethodBuilder.WithStateWithoutReturn);
				WriteMatchMethod(unionInfo, unionBodyBlock, MatchMethodBuilder.WithStateWithReturn);
			});

		return codeWriter.ToString();
	}

	private static void WriteUnionCase(UnionCaseInfo unionCaseInfo, UnionInfo unionInfo, CodeWriter unionBodyBlock)
	{
		var caseClassName = GetClassName(unionCaseInfo);
		unionBodyBlock.AppendLine($"private sealed class {caseClassName} : {unionInfo.Name}");
		var caseParametersString = string.Join(", ", unionCaseInfo.Parameters.Select(x => $"{x.TypeName} {x.Name}"));
		using (var caseClassBlock = unionBodyBlock.NewBlock())
		{
			foreach (var caseParameter in unionCaseInfo.Parameters)
			{
				caseClassBlock.AppendLine($"public readonly {caseParameter.TypeName} {caseParameter.Name};");
			}

			if (!unionCaseInfo.HasParameters)
			{
				caseClassBlock.AppendLine(
					$"public static readonly {caseClassName} Instance = new {caseClassName}();");
			}

			caseClassBlock.AppendLine($"public {caseClassName}({caseParametersString})");
			using var ctorBlock = caseClassBlock.NewBlock();
			foreach (var caseParameter in unionCaseInfo.Parameters)
			{
				ctorBlock.AppendLine($"this.{caseParameter.Name} = {caseParameter.Name};");
			}
		}

		unionBodyBlock.AppendLine($"public static partial {unionInfo.Name} {unionCaseInfo.Name}({caseParametersString})");
		using var methodBlock = unionBodyBlock.NewBlock();
		methodBlock.AppendLine(unionCaseInfo.HasParameters
			? $"return new {caseClassName}({string.Join(", ", unionCaseInfo.Parameters.Select(x => x.Name))});"
			: $"return {caseClassName}.Instance;");
	}

	private static void WriteMatchMethod(UnionInfo unionInfo, CodeWriter unionBodyBlock,
		MatchMethodBuilder methodBuilder)
	{
		var casesStrings = unionInfo.Cases
			.Select(x =>
			{
				var caseTypeParameters = x.HasParameters
					? $"{string.Join(",", x.Parameters.Select(y => y.TypeName))}"
					: string.Empty;
				var caseClassName = GetClassName(x);
				var caseClassNameCamelCase = $"{char.ToLowerInvariant(caseClassName[0])}{caseClassName.AsSpan(1).ToString()}";
				var matchHandlerParameterName = $"{caseClassNameCamelCase}Handler";
				return (
					caseClassName,
					matchHandlerParameterName,
					caseVariableName: $"{caseClassNameCamelCase}Var",
					matchHandlerParameter: $"{methodBuilder.MatchHandlerDelegateTypeProvider(caseTypeParameters)} {matchHandlerParameterName}");
			})
			.ToArray();

		var methodParameters =
			methodBuilder.MethodParametersProvider(string.Join(", ", casesStrings.Select(x => x.matchHandlerParameter)));
		unionBodyBlock.AppendLine($"public {methodBuilder.ReturnType} {methodBuilder.MethodName}({methodParameters})");
		using var methodBlock = unionBodyBlock.NewBlock();

		foreach (var (_, caseDelegateParameterName, _, _) in casesStrings)
		{
			methodBlock.AppendLine($"{ThrowIfNullMethod}({caseDelegateParameterName}, \"{caseDelegateParameterName}\");");
		}

		methodBlock.AppendLine();
		foreach (var ((caseClassName, matchHandlerParameterName, varName, _), unionCase) in casesStrings.Zip(unionInfo.Cases, (x, y) => (x, y)))
		{
			methodBlock.AppendLine($"var {varName} = this as {caseClassName};");
			var parametersString = string.Join(", ", unionCase.Parameters.Select(x => $"{varName}.{x.Name}"));
			methodBlock.AppendLine(
				$"if ({varName} != null) {{ {methodBuilder.MatchBodyProvider(matchHandlerParameterName, parametersString)} }}");
		}

		methodBlock.AppendLine($"{ThrowUnionInInvalidStateMethod}();");
		if (methodBuilder.ReturnType != "void")
		{
			methodBlock.AppendLine("return default;");
		}
	}

	private static string GetClassName(UnionCaseInfo unionCase) => $"{unionCase.Name}Case";

	private sealed class MatchMethodBuilder
	{
		private const string MatchMethodName = "Match";

		public static readonly MatchMethodBuilder WithoutStateWithoutReturn = new(
			"void",
			caseTypeParameters => string.IsNullOrEmpty(caseTypeParameters)
				? "System.Action"
				: $"System.Action<{caseTypeParameters}>",
			(matchHandlerParameterName, caseParameters) => $"{matchHandlerParameterName}({caseParameters}); return;");

		public static readonly MatchMethodBuilder WithoutStateWithReturn = new(
			"T",
			caseTypeParameters => string.IsNullOrEmpty(caseTypeParameters)
				? "System.Func<T>"
				: $"System.Func<{caseTypeParameters}, T>",
			(matchHandlerParameterName, caseParameters) => $"return {matchHandlerParameterName}({caseParameters});")
		{
			MethodName = $"{MatchMethodName}<T>",
		};

		public static readonly MatchMethodBuilder WithStateWithoutReturn = new(
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
			MethodName = $"{MatchMethodName}<TState>",
			MethodParametersProvider = matchDelegateParameters => $"TState state, {matchDelegateParameters}",
		};

		public static readonly MatchMethodBuilder WithStateWithReturn = new(
			"T",
			caseTypeParameters => string.IsNullOrEmpty(caseTypeParameters)
				? "System.Func<TState, T>"
				: $"System.Func<TState, {caseTypeParameters}, T>",
			(matchHandlerParameterName, caseParameters) =>
			{
				caseParameters = string.IsNullOrEmpty(caseParameters) ? string.Empty : $", {caseParameters}";
				return $"return {matchHandlerParameterName}(state{caseParameters});";
			})
		{
			MethodName = $"{MatchMethodName}<TState, T>",
			MethodParametersProvider = matchDelegateParameters => $"TState state, {matchDelegateParameters}",
		};

		public string ReturnType { get; }

		public string MethodName { get; private init; } = MatchMethodName;

		public Func<string, string> MatchHandlerDelegateTypeProvider { get; }

		public Func<string, string, string> MatchBodyProvider { get; }

		public Func<string, string> MethodParametersProvider { get; private init; } = x => x;

		public MatchMethodBuilder(string returnType, Func<string, string> matchHandlerDelegateTypeProvider,
			Func<string, string, string> matchBodyProvider)
		{
			ReturnType = returnType;
			MatchHandlerDelegateTypeProvider = matchHandlerDelegateTypeProvider;
			MatchBodyProvider = matchBodyProvider;
		}
	}
}