using System;
using System.Collections.Generic;
using System.Linq;
using Dusharp.CodeAnalyzing;
using Dusharp.CodeGeneration;
using Microsoft.CodeAnalysis;
using TypeKind = Dusharp.CodeGeneration.TypeKind;

namespace Dusharp.UnionGeneration;

public sealed class UnionCodeGenerator
{
	private static readonly string ThrowIfNullMethod =
		$"{typeof(ExceptionUtils).FullName}.{nameof(ExceptionUtils.ThrowIfNull)}";
	private static readonly string ThrowUnionInInvalidStateMethod =
		$"{typeof(ExceptionUtils).FullName}.{nameof(ExceptionUtils.ThrowUnionInInvalidState)}";

	private readonly TypeCodeWriter _typeCodeWriter;

	public UnionCodeGenerator(TypeCodeWriter typeCodeWriter)
	{
		_typeCodeWriter = typeCodeWriter;
	}

	public string? GenerateUnion(UnionInfo unionInfo)
	{
		return unionInfo.Cases.Count == 0 ? null : GenerateUnionImpl(new UnionGenerationInfo(unionInfo));
	}

	private string GenerateUnionImpl(UnionGenerationInfo unionInfo)
	{
		using var codeWriter = new CodeWriter();
		codeWriter.AppendLine("#nullable enable");

		CodeWritingUtils.WriteOuterBlocks(
			unionInfo.TypeSymbol, codeWriter,
			innerBlock =>
			{
				innerBlock.WriteSuppressWarning("CA1000", "For generic unions.");
				var unionTypeDefinition = new TypeDefinition
				{
					Name = unionInfo.Name,
					Kind = TypeKind.Class(true, false),
					IsPartial = true,
					GenericParameters = unionInfo.GenericParameters,
					InheritedTypes = [$"System.IEquatable<{unionInfo.ClassName}>"],
					Constructors =
					[
						new ConstructorDefinition
						{
							Accessibility = Accessibility.Private,
							BodyWriter = (_, _, _) => { },
						}
					],
					Methods =
					[
						.. unionInfo.Cases.Select(x => GetUnionCaseMethod(x, unionInfo)),
						GetMatchMethod(unionInfo, MatchMethodConfiguration.WithoutStateWithoutReturn),
						GetMatchMethod(unionInfo, MatchMethodConfiguration.WithoutStateWithReturn),
						GetMatchMethod(unionInfo, MatchMethodConfiguration.WithStateWithoutReturn),
						GetMatchMethod(unionInfo, MatchMethodConfiguration.WithStateWithReturn),
						.. GetDefaultEqualsMethods(unionInfo),
					],
					Operators = GetEqualityOperators(unionInfo),
					NestedTypes = unionInfo.Cases.Select(x => GetUnionCaseNestedType(x, unionInfo)).ToArray(),
				};

				_typeCodeWriter.WriteType(unionTypeDefinition, innerBlock);
			});

		return codeWriter.ToString();
	}

	private static MethodDefinition GetUnionCaseMethod(UnionCaseGenerationInfo unionCase, UnionGenerationInfo union) =>
		new MethodDefinition
		{
			Name = unionCase.Name,
			ReturnType = union.ClassName,
			Accessibility = Accessibility.Public,
			MethodModifier = MethodModifier.Static(),
			IsPartial = true,
			Parameters = unionCase.Parameters.Select(x => new MethodParameter(x.TypeName, x.Name)).ToArray(),
			BodyWriter = (_, _, methodBlock) => methodBlock.AppendLine(unionCase.HasParameters
				? $"return new {unionCase.ClassName}({string.Join(", ", unionCase.Parameters.Select(x => x.Name))});"
				: $"return {unionCase.ClassName}.Instance;"),
		};

	private static MethodDefinition GetMatchMethod(
		UnionGenerationInfo union, MatchMethodConfiguration methodConfiguration)
	{
		var methodParameters = union.Cases
			.Select(x => new MethodParameter(
				methodConfiguration.MatchCaseDelegateTypeProvider(string.Join(", ", x.Parameters.Select(y => y.TypeName))),
				x.ClassNameCamelCase));

		return new MethodDefinition
		{
			Name = "Match",
			ReturnType = methodConfiguration.ReturnType,
			Accessibility = Accessibility.Public,
			Parameters = methodConfiguration.MethodParametersExtender(methodParameters).ToArray(),
			GenericParameters = methodConfiguration.GenericParameters,
			BodyWriter = (_, _, methodBlock) =>
			{
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
						$"if (!object.ReferenceEquals(unionCase, null)) {{ {methodConfiguration.MatchBodyProvider(unionCase.ClassNameCamelCase, parametersString)} }}");
				}

				methodBlock.AppendLine($"{ThrowUnionInInvalidStateMethod}();");
				if (methodConfiguration.ReturnType != "void")
				{
					methodBlock.AppendLine("return default!;");
				}
			},
		};
	}

	private static MethodDefinition[] GetDefaultEqualsMethods(UnionGenerationInfo union) =>
	[
		new MethodDefinition
		{
			Name = "Equals",
			Accessibility = Accessibility.Public,
			ReturnType = "bool",
			Parameters = [new MethodParameter($"{union.ClassName}?", "other")],
			MethodModifier = MethodModifier.Virtual(),
			BodyWriter = static (_, _, methodBlock) => methodBlock.AppendLine("return object.ReferenceEquals(this, other);"),
		},
		new MethodDefinition
		{
			Name = "Equals",
			Accessibility = Accessibility.Public,
			ReturnType = "bool",
			Parameters = [new MethodParameter("object?", "other")],
			MethodModifier = MethodModifier.Override(),
			BodyWriter = static (_, _, methodBlock) => methodBlock.AppendLine("return object.ReferenceEquals(this, other);"),
		},
		new MethodDefinition
		{
			Name = "GetHashCode",
			Accessibility = Accessibility.Public,
			ReturnType = "int",
			MethodModifier = MethodModifier.Override(),
			BodyWriter = static (_, _, methodBlock) =>
				methodBlock.AppendLine("return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);"),
		},
	];

	private static OperatorDefinition[] GetEqualityOperators(UnionGenerationInfo union) =>
	[
		new OperatorDefinition
		{
			Name = "==",
			ReturnType = "bool",
			Parameters =
			[
				new MethodParameter($"{union.ClassName}?", "left"),
				new MethodParameter($"{union.ClassName}?", "right"),
			],
			BodyWriter = (_, _, operatorBodyBlock) => operatorBodyBlock.AppendLine(
				"return !object.ReferenceEquals(left, null) ? left.Equals(right) : object.ReferenceEquals(left, right);"),
		},
		new OperatorDefinition
		{
			Name = "!=",
			ReturnType = "bool",
			Parameters =
			[
				new MethodParameter($"{union.ClassName}?", "left"),
				new MethodParameter($"{union.ClassName}?", "right"),
			],
			BodyWriter = (_, _, operatorBodyBlock) => operatorBodyBlock.AppendLine(
				"return !object.ReferenceEquals(left, null) ? !left.Equals(right) : !object.ReferenceEquals(left, right);"),
		},
	];

	private static TypeDefinition GetUnionCaseNestedType(UnionCaseGenerationInfo unionCase, UnionGenerationInfo union)
	{
		var fields = unionCase.Parameters.Select(caseParameter => new FieldDefinition
		{
			Accessibility = Accessibility.Public,
			IsReadOnly = true,
			TypeName = caseParameter.TypeName,
			Name = caseParameter.Name,
		});
		fields = !unionCase.HasParameters
			? fields.Append(new FieldDefinition
			{
				Accessibility = Accessibility.Public,
				IsStatic = true,
				IsReadOnly = true,
				TypeName = unionCase.ClassName,
				Name = "Instance",
				Initializer = $"new {unionCase.ClassName}()",
			})
			: fields;

		return new TypeDefinition
		{
			Accessibility = Accessibility.Private,
			Kind = TypeKind.Class(false, true),
			Name = unionCase.ClassName,
			InheritedTypes = [union.ClassName],
			Fields = fields.ToArray(),
			Constructors =
			[
				new ConstructorDefinition
				{
					Accessibility = Accessibility.Public,
					Parameters = unionCase.Parameters.Select(x => new MethodParameter(x.TypeName, x.Name)).ToArray(),
					BodyWriter = static (ctorDef, _, ctorBlock) =>
					{
						foreach (var parameter in ctorDef.Parameters)
						{
							ctorBlock.AppendLine($"this.{parameter.Name} = {parameter.Name};");
						}
					},
				},
			],
			Methods =
			[
				GetUnionCaseToStringMethod(unionCase),
				.. GetUnionCaseEqualsMethods(unionCase, union),
			],
		};
	}

	private static MethodDefinition[] GetUnionCaseEqualsMethods(
		UnionCaseGenerationInfo unionCase, UnionGenerationInfo union)
	{
		if (!unionCase.HasParameters)
		{
			return [];
		}

		const string structuralEqualsMethod = "StructuralEquals";
		return
		[
			new MethodDefinition
			{
				Accessibility = Accessibility.Public,
				MethodModifier = MethodModifier.Override(),
				ReturnType = "bool",
				Name = "Equals",
				Parameters = [new MethodParameter($"{union.ClassName}?", "other")],
				BodyWriter = static (_, typeDef, methodBlock) => WriteEqualsMethodBody(typeDef.FullName, methodBlock),
			},
			new MethodDefinition
			{
				Accessibility = Accessibility.Public,
				MethodModifier = MethodModifier.Override(),
				ReturnType = "bool",
				Name = "Equals",
				Parameters = [new MethodParameter("object?", "other")],
				BodyWriter = static (_, typeDef, methodBlock) => WriteEqualsMethodBody(typeDef.FullName, methodBlock),
			},
			new MethodDefinition
			{
				Accessibility = Accessibility.Public,
				MethodModifier = MethodModifier.Override(),
				ReturnType = "int",
				Name = "GetHashCode",
				BodyWriter = (_, _, methodBlock) =>
				{
					var caseNameHashCode = $"\"{unionCase.Name}\".GetHashCode()";
					var hashCodes = unionCase.Parameters
						.Select(x =>
							$"System.Collections.Generic.EqualityComparer<{x.TypeName}>.Default.GetHashCode({x.Name}!)")
						.Append(caseNameHashCode);
					methodBlock.AppendLine(
						$"unchecked {{ return {string.Join(" * -1521134295 + ", hashCodes)}; }}");
				},
			},
			new MethodDefinition
			{
				Accessibility = Accessibility.Private,
				ReturnType = "bool",
				Name = structuralEqualsMethod,
				Parameters = [new MethodParameter(unionCase.ClassName, "other")],
				BodyWriter = (_, _, methodBlock) =>
				{
					var conditions = unionCase.Parameters
						.Select(x =>
							$"System.Collections.Generic.EqualityComparer<{x.TypeName}>.Default.Equals({x.Name}, other.{x.Name})");
					methodBlock.AppendLine($"return {string.Join(" && ", conditions)};");
				},
			},
		];

		static void WriteEqualsMethodBody(string caseClassName, CodeWriter methodBodyBlock)
		{
			methodBodyBlock.AppendLine("if (object.ReferenceEquals(this, other)) return true;");
			methodBodyBlock.AppendLine($"var otherCasted = other as {caseClassName};");
			methodBodyBlock.AppendLine("if (object.ReferenceEquals(otherCasted, null)) return false;");
			methodBodyBlock.AppendLine($"return {structuralEqualsMethod}(otherCasted);");
		}
	}

	private static MethodDefinition GetUnionCaseToStringMethod(UnionCaseGenerationInfo unionCase) =>
		new()
		{
			Accessibility = Accessibility.Public,
			MethodModifier = MethodModifier.Override(),
			ReturnType = "string",
			Name = "ToString",
			BodyWriter = (_, _, methodBlock) =>
			{
				var parametersStr = string.Join(", ", unionCase.Parameters.Select(x => $"{x.Name} = {{{x.Name}}}"));
				var resultStr = !string.IsNullOrEmpty(parametersStr)
					? $"$\"{unionCase.Name} {{{{ {parametersStr} }}}}\""
					: $"\"{unionCase.Name}\"";
				methodBlock.AppendLine($"return {resultStr};");
			},
		};

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