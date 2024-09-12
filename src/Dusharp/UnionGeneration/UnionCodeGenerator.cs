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
		using var codeWriter = CodeWritingUtils.WriteOuterBlocks(
			unionInfo.TypeSymbol,
			innerBlock =>
			{
				innerBlock.WriteSuppressWarning("CS0660, CS0661", "Equals overriden in derived classes.", false);
				innerBlock.WriteSuppressWarning("CA1067", "Equals overriden in derived classes.");

				innerBlock.AppendLine($"abstract partial class {unionInfo.Name} : System.IEquatable<{unionInfo.Name}>");
				using var unionBodyBlock = innerBlock.NewBlock();
				unionBodyBlock.AppendLine($"private {unionInfo.Name}() {{}}");
				unionBodyBlock.AppendLine();

				WriteMatchMethod(unionInfo, unionBodyBlock, MatchMethodBuilder.WithoutStateWithoutReturn);
				WriteMatchMethod(unionInfo, unionBodyBlock, MatchMethodBuilder.WithoutStateWithReturn);
				WriteMatchMethod(unionInfo, unionBodyBlock, MatchMethodBuilder.WithStateWithoutReturn);
				WriteMatchMethod(unionInfo, unionBodyBlock, MatchMethodBuilder.WithStateWithReturn);

				unionBodyBlock.AppendLine($"public abstract bool Equals({unionInfo.Name} other);");

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
		var casesStrings = union.Cases
			.Select(x =>
			{
				var matchHandlerParameterName = $"{x.ClassNameCamelCase}Handler";
				return (
					matchHandlerParameterName,
					matchHandlerParameter: $"{methodBuilder.MatchHandlerDelegateTypeProvider(x.ParameterTypes)} {matchHandlerParameterName}");
			})
			.ToArray();

		var methodParameters =
			methodBuilder.MethodParametersProvider(string.Join(", ", casesStrings.Select(x => x.matchHandlerParameter)));
		unionBodyBlock.AppendLine($"public {methodBuilder.ReturnType} {methodBuilder.MethodName}({methodParameters})");
		using var methodBlock = unionBodyBlock.NewBlock();

		foreach (var (matchHandlerParameterName, _) in casesStrings)
		{
			methodBlock.AppendLine($"{ThrowIfNullMethod}({matchHandlerParameterName}, \"{matchHandlerParameterName}\");");
		}

		methodBlock.AppendLine();
		foreach (var ((matchHandlerParameterName, _), unionCase) in casesStrings.Zip(union.Cases, (x, y) => (x, y)))
		{
			using var matchBlock = methodBlock.NewBlock();
			matchBlock.AppendLine($"var unionCase = this as {unionCase.ClassName};");
			var parametersString = string.Join(", ", unionCase.Parameters.Select(x => $"unionCase.{x.Name}"));
			matchBlock.AppendLine(
				$"if (!object.ReferenceEquals(unionCase, null)) {{ {methodBuilder.MatchBodyProvider(matchHandlerParameterName, parametersString)} }}");
		}

		methodBlock.AppendLine($"{ThrowUnionInInvalidStateMethod}();");
		if (methodBuilder.ReturnType != "void")
		{
			methodBlock.AppendLine("return default;");
		}
	}

	private static void WriteEqualityOperators(UnionGenerationInfo union, CodeWriter unionBodyBlock)
	{
		unionBodyBlock.AppendLine($"public static bool operator ==({union.Name} left, {union.Name} right)");
		using (var operatorBodyBlock = unionBodyBlock.NewBlock())
		{
			operatorBodyBlock.AppendLine(
				"return !object.ReferenceEquals(left, null) ? left.Equals(right) : object.ReferenceEquals(left, right);");
		}

		unionBodyBlock.AppendLine($"public static bool operator !=({union.Name} left, {union.Name} right)");
		using (var operatorBodyBlock = unionBodyBlock.NewBlock())
		{
			operatorBodyBlock.AppendLine(
				"return !object.ReferenceEquals(left, null) ? !left.Equals(right) : !object.ReferenceEquals(left, right);");
		}
	}

	private static void WriteUnionCase(UnionCaseGenerationInfo unionCase, UnionGenerationInfo union, CodeWriter unionBodyBlock)
	{
		unionBodyBlock.AppendLine($"private sealed class {unionCase.ClassName} : {union.Name}");
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

		unionBodyBlock.AppendLine($"public static partial {union.Name} {unionCase.Name}({unionCase.ParameterTypesAndNames})");
		using var methodBlock = unionBodyBlock.NewBlock();
		methodBlock.AppendLine(unionCase.HasParameters
			? $"return new {unionCase.ClassName}({unionCase.ParameterNames});"
			: $"return {unionCase.ClassName}.Instance;");
	}

	private static void WriteUnionCaseEqualsMethods(UnionCaseGenerationInfo unionCase, UnionGenerationInfo union,
		CodeWriter caseClassBlock)
	{
		const string structuralEqualsMethod = "StructuralEquals";

		caseClassBlock.AppendLine($"public override bool Equals({union.Name} other)");
		WriteEqualsMethodBody(unionCase, caseClassBlock);

		caseClassBlock.AppendLine("public override bool Equals(object other)");
		WriteEqualsMethodBody(unionCase, caseClassBlock);

		caseClassBlock.AppendLine("public override int GetHashCode()");
		using (var methodBodyBlock = caseClassBlock.NewBlock())
		{
			var caseNameHashCode = $"\"{unionCase.Name}\".GetHashCode()";
			if (!unionCase.HasParameters)
			{
				methodBodyBlock.AppendLine($"return {caseNameHashCode};");
			}
			else
			{
				var hashCodes = unionCase.Parameters
					.Select(x =>
						$"System.Collections.Generic.EqualityComparer<{x.TypeName}>.Default.GetHashCode({x.Name})")
					.Append(caseNameHashCode);
				methodBodyBlock.AppendLine($"unchecked {{ return {string.Join(" * -1521134295 + ", hashCodes)}; }}");
			}
		}

		caseClassBlock.AppendLine($"private bool {structuralEqualsMethod}({unionCase.ClassName} other)");
		using (var methodBodyBlock = caseClassBlock.NewBlock())
		{
			if (!unionCase.HasParameters)
			{
				methodBodyBlock.AppendLine("return true;");
			}
			else
			{
				var conditions = unionCase.Parameters
					.Select(x =>
						$"System.Collections.Generic.EqualityComparer<{x.TypeName}>.Default.Equals({x.Name}, other.{x.Name})");
				methodBodyBlock.AppendLine($"return {string.Join(" && ", conditions)};");
			}
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