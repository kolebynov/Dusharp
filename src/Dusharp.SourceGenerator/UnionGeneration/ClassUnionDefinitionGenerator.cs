using System;
using System.Collections.Generic;
using System.Linq;
using Dusharp.CodeAnalyzing;
using Dusharp.CodeGeneration;
using Microsoft.CodeAnalysis;
using TypeKind = Dusharp.CodeGeneration.TypeKind;

namespace Dusharp.UnionGeneration;

public sealed class ClassUnionDefinitionGenerator : IUnionDefinitionGenerator
{
	private readonly UnionInfo _union;
	private Dictionary<string, TypeDefinition>? _nestedUnionCaseTypes;

	public ClassUnionDefinitionGenerator(UnionInfo union)
	{
		_union = union;
	}

	public TypeKind TypeKind => TypeKind.Class(true, false);

	public Action<MethodDefinition, CodeWriter> GetUnionCaseMethodBodyWriter(
		UnionCaseInfo unionCase, string unionTypeName)
	{
		var nestedUnionCaseType = GetUnionCaseNestedTypes(unionTypeName)[unionCase.Name];
		return (_, methodBlock) => methodBlock.AppendLine(unionCase.HasParameters
			? $"return new {nestedUnionCaseType.FullName}({string.Join(", ", unionCase.Parameters.Select(x => x.Name))});"
			: $"return {nestedUnionCaseType.FullName}.Instance;");
	}

	public MethodDefinition AdjustDefaultEqualsMethod(MethodDefinition equalsMethod) =>
		equalsMethod with
		{
			BodyWriter = static (def, methodBlock) =>
				methodBlock.AppendLine($"return object.ReferenceEquals(this, {def.Parameters[0].Name});"),
		};

	public MethodDefinition AdjustSpecificEqualsMethod(MethodDefinition equalsMethod) =>
		equalsMethod with
		{
			MethodModifier = MethodModifier.Virtual(),
			BodyWriter = static (def, methodBlock) =>
				methodBlock.AppendLine($"return object.ReferenceEquals(this, {def.Parameters[0].Name});"),
		};

	public Action<MethodDefinition, CodeWriter> GetGetHashCodeMethodBodyWriter() =>
		static (_, methodBlock) =>
			methodBlock.AppendLine("return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);");

	public Action<OperatorDefinition, CodeWriter> GetEqualityOperatorBodyWriter() =>
		static (def, operatorBodyBlock) =>
		{
			var leftName = def.Parameters[0].Name;
			var rightName = def.Parameters[1].Name;
			operatorBodyBlock.AppendLine(
				$"return !object.ReferenceEquals({leftName}, null) ? left.Equals({rightName}) : object.ReferenceEquals({leftName}, {rightName});");
		};

	public void WriteMatchBlock(UnionCaseInfo unionCase, Func<string, string> matchedCaseDelegateCallProvider,
		CodeWriter matchBlock, string unionTypeName)
	{
		var caseNestedType = GetUnionCaseNestedTypes(unionTypeName)[unionCase.Name];
		matchBlock.AppendLine($"var unionCase = this as {caseNestedType.FullName};");
		var argumentsStr = string.Join(", ", unionCase.Parameters.Select(x => $"unionCase.{x.Name}"));
		matchBlock.AppendLine(
			$"if (!object.ReferenceEquals(unionCase, null)) {{ {matchedCaseDelegateCallProvider(argumentsStr)} }}");
	}

	public TypeDefinition AddAdditionalInfo(TypeDefinition typeDefinition) =>
		typeDefinition with
		{
			Constructors =
			[
				new ConstructorDefinition
				{
					Accessibility = Accessibility.Private,
					BodyWriter = (_, _) => { },
				},
			],
			NestedTypes = GetUnionCaseNestedTypes(typeDefinition.FullName).Select(x => x.Value).ToArray(),
		};

	private Dictionary<string, TypeDefinition> GetUnionCaseNestedTypes(string unionTypeName) =>
		_nestedUnionCaseTypes ??= _union.Cases.ToDictionary(x => x.Name, x => GetUnionCaseNestedType(x, unionTypeName));

	private static TypeDefinition GetUnionCaseNestedType(UnionCaseInfo unionCase, string unionTypeName)
	{
		var unionCaseClassName = $"{unionCase.Name}Case";
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
				TypeName = unionCaseClassName,
				Name = "Instance",
				Initializer = $"new {unionCaseClassName}()",
			})
			: fields;

		return new TypeDefinition
		{
			Accessibility = Accessibility.Private,
			Kind = TypeKind.Class(false, true),
			Name = unionCaseClassName,
			InheritedTypes = [unionTypeName],
			Fields = fields.ToArray(),
			Constructors =
			[
				new ConstructorDefinition
				{
					Accessibility = Accessibility.Public,
					Parameters = unionCase.Parameters.Select(x => new MethodParameter(x.TypeName, x.Name)).ToArray(),
					BodyWriter = static (ctorDef, ctorBlock) =>
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
				.. GetUnionCaseEqualsMethods(unionCase, unionCaseClassName, unionTypeName),
			],
		};
	}

	private static MethodDefinition[] GetUnionCaseEqualsMethods(
		UnionCaseInfo unionCase, string caseClassName, string unionTypeName)
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
				Parameters = [new MethodParameter($"{unionTypeName}?", "other")],
				BodyWriter = (_, methodBlock) => WriteEqualsMethodBody(caseClassName, methodBlock),
			},
			new MethodDefinition
			{
				Accessibility = Accessibility.Public,
				MethodModifier = MethodModifier.Override(),
				ReturnType = "bool",
				Name = "Equals",
				Parameters = [new MethodParameter("object?", "other")],
				BodyWriter = (_, methodBlock) => WriteEqualsMethodBody(caseClassName, methodBlock),
			},
			new MethodDefinition
			{
				Accessibility = Accessibility.Public,
				MethodModifier = MethodModifier.Override(),
				ReturnType = "int",
				Name = "GetHashCode",
				BodyWriter = (_, methodBlock) =>
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
				Parameters = [new MethodParameter(caseClassName, "other")],
				BodyWriter = (_, methodBlock) =>
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

	private static MethodDefinition GetUnionCaseToStringMethod(UnionCaseInfo unionCase) =>
		new()
		{
			Accessibility = Accessibility.Public,
			MethodModifier = MethodModifier.Override(),
			ReturnType = "string",
			Name = "ToString",
			BodyWriter = (_, methodBlock) =>
			{
				var parametersStr = string.Join(", ", unionCase.Parameters.Select(x => $"{x.Name} = {{{x.Name}}}"));
				var resultStr = !string.IsNullOrEmpty(parametersStr)
					? $"$\"{unionCase.Name} {{{{ {parametersStr} }}}}\""
					: $"\"{unionCase.Name}\"";
				methodBlock.AppendLine($"return {resultStr};");
			},
		};
}