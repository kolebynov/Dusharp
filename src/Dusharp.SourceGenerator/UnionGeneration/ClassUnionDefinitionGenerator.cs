using System;
using System.Collections.Generic;
using System.Linq;
using Dusharp.CodeAnalyzing;
using Dusharp.CodeGeneration;
using Dusharp.Extensions;
using Microsoft.CodeAnalysis;
using TypeInfo = Dusharp.CodeAnalyzing.TypeInfo;
using TypeKind = Dusharp.CodeGeneration.TypeKind;

namespace Dusharp.UnionGeneration;

public sealed class ClassUnionDefinitionGenerator : IUnionDefinitionGenerator
{
	private readonly Dictionary<UnionCaseInfo, UnionCaseNestedType> _unionCaseNestedTypes;

	public ClassUnionDefinitionGenerator(UnionInfo union)
	{
		_unionCaseNestedTypes = GetUnionCaseNestedTypes(union);
	}

	public TypeKind TypeKind => TypeKind.Class(true, false);

	public Action<MethodDefinition, CodeWriter> GetUnionCaseMethodBodyWriter(UnionCaseInfo unionCase)
	{
		var caseNestedTypeInfo = _unionCaseNestedTypes[unionCase].TypeInfo;
		return (def, methodBlock) => methodBlock.AppendLine(unionCase.HasParameters
			? $"return new {caseNestedTypeInfo.GetFullyQualifiedName(false)}({string.Join(", ", def.Parameters.Select(x => x.Name))});"
			: $"return {caseNestedTypeInfo.GetFullyQualifiedName(false)}.Instance;");
	}

	public string GetUnionCaseCheckExpression(UnionCaseInfo unionCase)
	{
		var caseNestedTypeInfo = _unionCaseNestedTypes[unionCase].TypeInfo;
		return $"this is {caseNestedTypeInfo.GetFullyQualifiedName(false)}";
	}

	public IEnumerable<string> GetUnionCaseParameterAccessors(UnionCaseInfo unionCase)
	{
		var caseNestedTypeInfo = _unionCaseNestedTypes[unionCase].TypeInfo;
		return unionCase.Parameters
			.Select(x => $"{TypeInfos.Unsafe}.As<{caseNestedTypeInfo.GetFullyQualifiedName(false)}>(this).{x.Name}");
	}

	public MethodDefinition AdjustDefaultEqualsMethod(MethodDefinition equalsMethod) =>
		equalsMethod with
		{
			BodyWriter = static (def, methodBlock) =>
				methodBlock.AppendLine($"return {TypeInfos.Object}.ReferenceEquals(this, {def.Parameters[0].Name});"),
		};

	public MethodDefinition AdjustSpecificEqualsMethod(MethodDefinition equalsMethod) =>
		equalsMethod with
		{
			MethodModifier = MethodModifier.Virtual(),
			BodyWriter = static (def, methodBlock) =>
				methodBlock.AppendLine($"return {TypeInfos.Object}.ReferenceEquals(this, {def.Parameters[0].Name});"),
		};

	public Action<MethodDefinition, CodeWriter> GetGetHashCodeMethodBodyWriter() =>
		static (_, methodBlock) => methodBlock.AppendLine("return 0;");

	public Action<OperatorDefinition, CodeWriter> GetEqualityOperatorBodyWriter() =>
		static (def, operatorBodyBlock) =>
		{
			var leftName = def.Parameters[0].Name;
			var rightName = def.Parameters[1].Name;
			operatorBodyBlock.AppendLine(
				$"return !{TypeInfos.Object}.ReferenceEquals({leftName}, null) ? left.Equals({rightName}) : {TypeInfos.Object}.ReferenceEquals({leftName}, {rightName});");
		};

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
			NestedTypes = _unionCaseNestedTypes.Select(x => x.Value.TypeDefinition).ToArray(),
		};

	public IReadOnlyList<TypeDefinition> GetAdditionalTypes() => [];

	private static Dictionary<UnionCaseInfo, UnionCaseNestedType> GetUnionCaseNestedTypes(UnionInfo union) =>
		union.Cases.ToDictionary(x => x, x => GetUnionCaseNestedType(x, union));

	private static UnionCaseNestedType GetUnionCaseNestedType(UnionCaseInfo unionCase, UnionInfo union)
	{
		var caseClassName = $"{unionCase.Name}Case";
		var caseTypeInfo = TypeInfo.SpecificType(union.TypeInfo.Namespace, union.TypeInfo,
			caseClassName, TypeInfo.TypeKind.ReferenceType(false));

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
				TypeName = new TypeName(caseTypeInfo, false),
				Name = "Instance",
				Initializer = $"new {caseTypeInfo.GetFullyQualifiedName(false)}()",
			})
			: fields;

		var typeDefinition = new TypeDefinition
		{
			Accessibility = Accessibility.Private,
			Kind = TypeKind.Class(false, true),
			Name = caseClassName,
			InheritedTypes = [union.TypeInfo],
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
				.. GetUnionCaseEqualsMethods(unionCase, caseTypeInfo, union),
			],
		};

		return new UnionCaseNestedType(caseTypeInfo, typeDefinition);
	}

	private static MethodDefinition[] GetUnionCaseEqualsMethods(
		UnionCaseInfo unionCase, TypeInfo caseTypeInfo, UnionInfo union)
	{
		var getHashCodeMethodDefinition = new MethodDefinition
		{
			Accessibility = Accessibility.Public,
			MethodModifier = MethodModifier.Override(),
			ReturnType = TypeNames.Int32,
			Name = "GetHashCode",
			BodyWriter = (_, methodBlock) =>
			{
				var caseIndex = union.Cases.IndexOf(unionCase) + 1;
				methodBlock.AppendLine(
					UnionGenerationUtils.GetUnionCaseHashCodeCode(
						caseIndex, unionCase.Parameters.Select(x => (x.TypeName, x.Name))));
			},
		};

		if (!unionCase.HasParameters)
		{
			return [getHashCodeMethodDefinition];
		}

		const string structuralEqualsMethod = "StructuralEquals";
		return
		[
			new MethodDefinition
			{
				Accessibility = Accessibility.Public,
				MethodModifier = MethodModifier.Override(),
				ReturnType = TypeNames.Boolean,
				Name = "Equals",
				Parameters = [new MethodParameter(new TypeName(union.TypeInfo, true), "other")],
				BodyWriter = (_, methodBlock) => WriteEqualsMethodBody(caseTypeInfo, methodBlock),
			},
			new MethodDefinition
			{
				Accessibility = Accessibility.Public,
				MethodModifier = MethodModifier.Override(),
				ReturnType = TypeNames.Boolean,
				Name = "Equals",
				Parameters = [new MethodParameter(TypeNames.Object(true), "other")],
				BodyWriter = (_, methodBlock) => WriteEqualsMethodBody(caseTypeInfo, methodBlock),
			},
			getHashCodeMethodDefinition,
			new MethodDefinition
			{
				Accessibility = Accessibility.Private,
				ReturnType = TypeNames.Boolean,
				Name = structuralEqualsMethod,
				Parameters = [new MethodParameter(new TypeName(caseTypeInfo, false), "other")],
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

		static void WriteEqualsMethodBody(TypeInfo caseTypeInfo, CodeWriter methodBodyBlock)
		{
			methodBodyBlock.AppendLine($"if ({TypeInfos.Object}.ReferenceEquals(this, other)) return true;");
			methodBodyBlock.AppendLine($"var otherCasted = other as {caseTypeInfo.GetFullyQualifiedName(false)};");
			methodBodyBlock.AppendLine($"if ({TypeInfos.Object}.ReferenceEquals(otherCasted, null)) return false;");
			methodBodyBlock.AppendLine($"return {structuralEqualsMethod}(otherCasted);");
		}
	}

	private static MethodDefinition GetUnionCaseToStringMethod(UnionCaseInfo unionCase) =>
		new()
		{
			Accessibility = Accessibility.Public,
			MethodModifier = MethodModifier.Override(),
			ReturnType = TypeNames.String(),
			Name = "ToString",
			BodyWriter = (_, methodBlock) => methodBlock
				.Append("return ")
				.Append(UnionGenerationUtils.GetCaseStringRepresentation(
					unionCase.Name, unionCase.Parameters.Select(x => (x.Name, x.Name)).ToArray()))
				.AppendLine(";"),
		};

	private readonly record struct UnionCaseNestedType(TypeInfo TypeInfo, TypeDefinition TypeDefinition);
}