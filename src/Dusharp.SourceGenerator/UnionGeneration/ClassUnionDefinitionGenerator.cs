using System;
using System.Collections.Generic;
using System.Linq;
using Dusharp.CodeAnalyzing;
using Dusharp.CodeGeneration;
using Dusharp.Extensions;
using Microsoft.CodeAnalysis;
using TypeKind = Dusharp.CodeGeneration.TypeKind;

namespace Dusharp.UnionGeneration;

public sealed class ClassUnionDefinitionGenerator : IUnionDefinitionGenerator
{
	private readonly Dictionary<UnionCaseInfo, TypeDefinition> _nestedUnionCaseTypes;

	public ClassUnionDefinitionGenerator(UnionInfo union)
	{
		_nestedUnionCaseTypes = GetUnionCaseNestedTypes(union, union.GetTypeName().FullName);
	}

	public TypeKind TypeKind => TypeKind.Class(true, false);

	public Action<MethodDefinition, CodeWriter> GetUnionCaseMethodBodyWriter(UnionCaseInfo unionCase)
	{
		var nestedUnionCaseType = _nestedUnionCaseTypes[unionCase];
		return (def, methodBlock) => methodBlock.AppendLine(unionCase.HasParameters
			? $"return new {nestedUnionCaseType.FullName}({string.Join(", ", def.Parameters.Select(x => x.Name))});"
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
		CodeWriter matchBlock)
	{
		var caseNestedType = _nestedUnionCaseTypes[unionCase];
		matchBlock.AppendLine($"var unionCase = this as {caseNestedType.FullName};");
		var argumentsStr = string.Join(", ", unionCase.Parameters.Select(x => $"unionCase.{x.Name}"));
		matchBlock.AppendLine(
			$"if (!object.ReferenceEquals(unionCase, null)) {{ {matchedCaseDelegateCallProvider(argumentsStr)} }}");
	}

	public TypeDefinition AdjustUnionTypeDefinition(TypeDefinition typeDefinition) =>
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
			NestedTypes = _nestedUnionCaseTypes.Select(x => x.Value).ToArray(),
		};

	public IReadOnlyList<TypeDefinition> GetAdditionalTypes() => [];

	private static Dictionary<UnionCaseInfo, TypeDefinition> GetUnionCaseNestedTypes(UnionInfo union, string unionTypeName) =>
		union.Cases.ToDictionary(x => x, x => GetUnionCaseNestedType(x, union, unionTypeName));

	private static TypeDefinition GetUnionCaseNestedType(UnionCaseInfo unionCase, UnionInfo union, string unionTypeName)
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
				.. GetUnionCaseEqualsMethods(unionCase, union, unionCaseClassName, unionTypeName),
			],
		};
	}

	private static MethodDefinition[] GetUnionCaseEqualsMethods(
		UnionCaseInfo unionCase, UnionInfo union, string caseClassName, string unionTypeName)
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
					var caseIndex = union.Cases.IndexOf(unionCase) + 1;
					methodBlock.AppendLine(
						UnionGenerationUtils.GetUnionCaseHashCodeCode(
							caseIndex, unionCase.Parameters.Select(x => (x.TypeName, x.Name))));
				},
			},
			new MethodDefinition
			{
				Accessibility = Accessibility.Private,
				ReturnType = "bool",
				Name = structuralEqualsMethod,
				Parameters = [new MethodParameter(caseClassName, "other")],
				BodyWriter = (def, methodBlock) =>
				{
					methodBlock
						.Append("return ")
						.Append(UnionGenerationUtils.GetUnionCaseEqualityCode(
							unionCase.Parameters.Select(x => (x.TypeName, x.Name, $"{def.Parameters[0].Name}.{x.Name}"))))
						.AppendLine(";");
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
			BodyWriter = (_, methodBlock) => methodBlock
				.Append("return ")
				.Append(UnionGenerationUtils.GetCaseStringRepresentation(
					unionCase.Name, unionCase.Parameters.Select(x => (x.Name, x.Name)).ToArray()))
				.AppendLine(";"),
		};
}