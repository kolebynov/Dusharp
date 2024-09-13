using System;
using System.Linq;
using Dusharp.CodeAnalyzing;

namespace Dusharp.UnionGeneration;

public static class UnionCodeGenerator
{
	private static readonly string ThrowIfNullMethod =
		$"{typeof(ExceptionUtils).FullName}.{nameof(ExceptionUtils.ThrowIfNull)}";
	private static readonly string ThrowUnionInInvalidStateMethod =
		$"{typeof(ExceptionUtils).FullName}.{nameof(ExceptionUtils.ThrowUnionInInvalidState)}";

	public static string? GenerateClassUnion(UnionInfo unionInfo)
	{
		return unionInfo.Cases.Count == 0 ? null : GenerateClassUnionImpl(new UnionGenerationInfo(unionInfo));
	}

	private static string GenerateClassUnionImpl(UnionGenerationInfo unionInfo)
	{
		using var codeWriter = new CodeWriter();
		codeWriter.AppendLine("#nullable enable");

		CodeWritingUtils.WriteOuterBlocks(
			unionInfo.TypeSymbol, codeWriter,
			innerBlock =>
			{
				innerBlock.WriteSuppressWarning("CA1000", "For generic unions.");

				innerBlock.AppendLine($"abstract partial class {unionInfo.ClassName} : System.IEquatable<{unionInfo.ClassName}>");
				using var unionBodyBlock = innerBlock.NewBlock();
				unionBodyBlock.AppendLine($"private {unionInfo.Name}() {{}}");
				unionBodyBlock.AppendLine();

				WriteMatchMethod(unionInfo, unionBodyBlock, MatchMethodBuilder.WithoutStateWithoutReturn);
				WriteMatchMethod(unionInfo, unionBodyBlock, MatchMethodBuilder.WithoutStateWithReturn);
				WriteMatchMethod(unionInfo, unionBodyBlock, MatchMethodBuilder.WithStateWithoutReturn);
				WriteMatchMethod(unionInfo, unionBodyBlock, MatchMethodBuilder.WithStateWithReturn);

				WriteDefaultEqualsMethods(unionInfo, unionBodyBlock);
				WriteEqualityOperators(unionInfo, unionBodyBlock);

				foreach (var caseMethodSymbol in unionInfo.Cases)
				{
					WriteUnionCase(caseMethodSymbol, unionInfo, unionBodyBlock);
				}
			});

		return codeWriter.ToString();
	}

	private static void WriteMatchMethod(UnionGenerationInfo union, CodeWriter unionBodyBlock,
		MatchMethodBuilder methodBuilder)
	{
		var methodParameters = union.Cases
			.Select(x => $"{methodBuilder.MatchHandlerDelegateTypeProvider(x.ParameterTypes)} {x.ClassNameCamelCase}");
		var methodParametersStr =
			methodBuilder.MethodParametersProvider(string.Join(", ", methodParameters));
		unionBodyBlock.AppendLine($"public {methodBuilder.ReturnType} {methodBuilder.MethodName}({methodParametersStr})");

		using var methodBlock = unionBodyBlock.NewBlock();

		foreach (var unionCase in union.Cases)
		{
			methodBlock.AppendLine($"{ThrowIfNullMethod}({unionCase.ClassNameCamelCase}, \"{unionCase.ClassNameCamelCase}\");");
		}

		methodBlock.AppendLine();
		foreach (var unionCase in union.Cases)
		{
			using var matchBlock = methodBlock.NewBlock();
			matchBlock.AppendLine($"var unionCase = this as {unionCase.ClassName};");
			var parametersString = string.Join(", ", unionCase.Parameters.Select(x => $"unionCase.{x.Name}"));
			matchBlock.AppendLine(
				$"if (!object.ReferenceEquals(unionCase, null)) {{ {methodBuilder.MatchBodyProvider(unionCase.ClassNameCamelCase, parametersString)} }}");
		}

		methodBlock.AppendLine($"{ThrowUnionInInvalidStateMethod}();");
		if (methodBuilder.ReturnType != "void")
		{
			methodBlock.AppendLine("return default!;");
		}
	}

	private static void WriteDefaultEqualsMethods(UnionGenerationInfo union, CodeWriter unionBodyBlock)
	{
		unionBodyBlock.AppendLine($"public virtual bool Equals({union.ClassName}? other) {{ return object.ReferenceEquals(this, other); }}");
		unionBodyBlock.AppendLine("public override bool Equals(object? other) { return object.ReferenceEquals(this, other); }");
		unionBodyBlock.AppendLine("public override int GetHashCode() { return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this); }");
	}

	private static void WriteEqualityOperators(UnionGenerationInfo union, CodeWriter unionBodyBlock)
	{
		unionBodyBlock.AppendLine($"public static bool operator ==({union.ClassName}? left, {union.ClassName}? right)");
		using (var operatorBodyBlock = unionBodyBlock.NewBlock())
		{
			operatorBodyBlock.AppendLine(
				"return !object.ReferenceEquals(left, null) ? left.Equals(right) : object.ReferenceEquals(left, right);");
		}

		unionBodyBlock.AppendLine($"public static bool operator !=({union.ClassName}? left, {union.ClassName}? right)");
		using (var operatorBodyBlock = unionBodyBlock.NewBlock())
		{
			operatorBodyBlock.AppendLine(
				"return !object.ReferenceEquals(left, null) ? !left.Equals(right) : !object.ReferenceEquals(left, right);");
		}
	}

	private static void WriteUnionCase(UnionCaseGenerationInfo unionCase, UnionGenerationInfo union, CodeWriter unionBodyBlock)
	{
		unionBodyBlock.AppendLine($"private sealed class {unionCase.ClassName} : {union.ClassName}");
		using (var caseClassBlock = unionBodyBlock.NewBlock())
		{
			foreach (var caseParameter in unionCase.Parameters)
			{
				caseClassBlock.AppendLine($"public readonly {caseParameter.TypeName} {caseParameter.Name};");
			}

			if (!unionCase.HasParameters)
			{
				caseClassBlock.AppendLine(
					$"public static readonly {unionCase.ClassName} Instance = new {unionCase.ClassName}();");
			}

			caseClassBlock.AppendLine($"public {unionCase.ClassName}({unionCase.ParameterTypesAndNames})");
			using (var ctorBlock = caseClassBlock.NewBlock())
			{
				foreach (var caseParameter in unionCase.Parameters)
				{
					ctorBlock.AppendLine($"this.{caseParameter.Name} = {caseParameter.Name};");
				}
			}

			WriteUnionCaseEqualsMethods(unionCase, union, caseClassBlock);
		}

		unionBodyBlock.AppendLine($"public static partial {union.ClassName} {unionCase.Name}({unionCase.ParameterTypesAndNames})");
		using var methodBlock = unionBodyBlock.NewBlock();
		methodBlock.AppendLine(unionCase.HasParameters
			? $"return new {unionCase.ClassName}({unionCase.ParameterNames});"
			: $"return {unionCase.ClassName}.Instance;");
	}

	private static void WriteUnionCaseEqualsMethods(UnionCaseGenerationInfo unionCase, UnionGenerationInfo union,
		CodeWriter caseClassBlock)
	{
		if (!unionCase.HasParameters)
		{
			return;
		}

		const string structuralEqualsMethod = "StructuralEquals";

		caseClassBlock.AppendLine($"public override bool Equals({union.ClassName}? other)");
		WriteEqualsMethodBody(unionCase, caseClassBlock);

		caseClassBlock.AppendLine("public override bool Equals(object? other)");
		WriteEqualsMethodBody(unionCase, caseClassBlock);

		caseClassBlock.AppendLine("public override int GetHashCode()");
		using (var methodBodyBlock = caseClassBlock.NewBlock())
		{
			var caseNameHashCode = $"\"{unionCase.Name}\".GetHashCode()";
			var hashCodes = unionCase.Parameters
				.Select(x =>
					$"System.Collections.Generic.EqualityComparer<{x.TypeName}>.Default.GetHashCode({x.Name}!)")
				.Append(caseNameHashCode);
			methodBodyBlock.AppendLine(
				$"unchecked {{ return {string.Join(" * -1521134295 + ", hashCodes)}; }}");
		}

		caseClassBlock.AppendLine($"private bool {structuralEqualsMethod}({unionCase.ClassName} other)");
		using (var methodBodyBlock = caseClassBlock.NewBlock())
		{
			var conditions = unionCase.Parameters
				.Select(x =>
					$"System.Collections.Generic.EqualityComparer<{x.TypeName}>.Default.Equals({x.Name}, other.{x.Name})");
			methodBodyBlock.AppendLine($"return {string.Join(" && ", conditions)};");
		}

		return;

		static void WriteEqualsMethodBody(UnionCaseGenerationInfo unionCase, CodeWriter caseClassBlock)
		{
			using var methodBodyBlock = caseClassBlock.NewBlock();
			methodBodyBlock.AppendLine("if (object.ReferenceEquals(this, other)) return true;");
			methodBodyBlock.AppendLine($"var otherCasted = other as {unionCase.ClassName};");
			methodBodyBlock.AppendLine("if (object.ReferenceEquals(otherCasted, null)) return false;");
			methodBodyBlock.AppendLine($"return {structuralEqualsMethod}(otherCasted);");
		}
	}

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
			"TRet",
			caseTypeParameters => string.IsNullOrEmpty(caseTypeParameters)
				? "System.Func<TRet>"
				: $"System.Func<{caseTypeParameters}, TRet>",
			(matchHandlerParameterName, caseParameters) => $"return {matchHandlerParameterName}({caseParameters});")
		{
			MethodName = $"{MatchMethodName}<TRet>",
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
			MethodName = $"{MatchMethodName}<TState, TRet>",
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