using System;
using System.Collections.Generic;
using System.Linq;

namespace Dusharp.CodeGeneration;

public sealed class TypeCodeWriter
{
#pragma warning disable CA1822
	public void WriteType(TypeDefinition typeDefinition, CodeWriter codeWriter)
#pragma warning restore CA1822
	{
		foreach (var attribute in typeDefinition.Attributes)
		{
			WriteAttribute(attribute, codeWriter);
		}

		WriteTypeDeclaration(typeDefinition, codeWriter);

		using var typeBodyBlock = codeWriter.NewBlock();

		foreach (var fieldDefinition in typeDefinition.Fields)
		{
			WriteField(fieldDefinition, typeBodyBlock);
		}

		foreach (var propertyDefinition in typeDefinition.Properties)
		{
			WriteProperty(propertyDefinition, typeBodyBlock);
		}

		foreach (var constructorDefinition in typeDefinition.Constructors)
		{
			WriteConstructor(constructorDefinition, typeDefinition, typeBodyBlock);
		}

		foreach (var methodDefinition in typeDefinition.Methods)
		{
			WriteMethod(methodDefinition, typeBodyBlock);
		}

		foreach (var operatorDefinition in typeDefinition.Operators)
		{
			WriteOperator(operatorDefinition, typeBodyBlock);
		}

		foreach (var nestedTypeDefinition in typeDefinition.NestedTypes)
		{
			WriteType(nestedTypeDefinition, typeBodyBlock);
		}
	}

	private static void WriteTypeDeclaration(TypeDefinition typeDefinition, CodeWriter codeWriter)
	{
		var declarationBuilder = new DeclarationBuilder()
			.AddIf(typeDefinition.Accessibility != null, () => typeDefinition.Accessibility!.Value.ToCodeString());

		typeDefinition.Kind
			.Match(
				declarationBuilder,
				static (declarationBuilder, isAbstract, isSealed) => declarationBuilder
					.AddIf(isAbstract, () => "abstract")
					.AddIf(isSealed, () => "sealed"),
				static (declarationBuilder, isReadOnly) => declarationBuilder
					.AddIf(isReadOnly, () => "readonly"))
			.AddIf(typeDefinition.IsPartial, () => "partial");

		typeDefinition.Kind.Match(
			declarationBuilder,
			static (declarationBuilder, _, _) => declarationBuilder.Add("class"),
			static (declarationBuilder, _) => declarationBuilder.Add("struct"));

		declarationBuilder
			.Add(typeDefinition.Name)
			.AddIf(
				typeDefinition.InheritedTypes.Count > 0,
				() => $": {string.Join(", ", typeDefinition.InheritedTypes)}");

		codeWriter.AppendLine(declarationBuilder.ToString());
	}

	private static void WriteField(FieldDefinition fieldDefinition, CodeWriter typeBodyBlock)
	{
		foreach (var attribute in fieldDefinition.Attributes)
		{
			WriteAttribute(attribute, typeBodyBlock);
		}

		var declarationBuilder = new DeclarationBuilder()
			.AddIf(fieldDefinition.Accessibility != null, () => fieldDefinition.Accessibility!.Value.ToCodeString())
			.AddIf(fieldDefinition.IsStatic, () => "static")
			.AddIf(fieldDefinition.IsReadOnly, () => "readonly")
			.Add(fieldDefinition.TypeName.FullyQualifiedName)
			.Add(fieldDefinition.Name)
			.AddIf(fieldDefinition.Initializer != null, () => "=", () => fieldDefinition.Initializer!);
		typeBodyBlock.AppendLine($"{declarationBuilder};");
	}

	private static void WriteProperty(PropertyDefinition propertyDefinition, CodeWriter typeBodyBlock)
	{
		foreach (var attribute in propertyDefinition.Attributes)
		{
			WriteAttribute(attribute, typeBodyBlock);
		}

		var declarationBuilder = new DeclarationBuilder()
			.AddIf(propertyDefinition.Accessibility != null, () => propertyDefinition.Accessibility!.Value.ToCodeString())
			.AddIf(propertyDefinition.IsStatic, () => "static")
			.Add(propertyDefinition.TypeName.FullyQualifiedName)
			.Add(propertyDefinition.Name);
		typeBodyBlock.AppendLine(declarationBuilder.ToString());
		using (var propertyBodyBlock = typeBodyBlock.NewBlock())
		{
			if (propertyDefinition.Getter != null)
			{
				WritePropertyAccessor(propertyDefinition.Getter.Value, "get", propertyBodyBlock, propertyDefinition);
			}

			if (propertyDefinition.Setter != null)
			{
				WritePropertyAccessor(propertyDefinition.Setter.Value, "set", propertyBodyBlock, propertyDefinition);
			}
		}

		if (propertyDefinition.Initializer != null)
		{
			typeBodyBlock.AppendLine($" = {propertyDefinition.Initializer};");
		}

		return;

		static void WritePropertyAccessor(PropertyDefinition.PropertyAccessor propertyAccessor, string accessorName,
			CodeWriter propertyBodyBlock, PropertyDefinition propertyDefinition)
		{
			if (propertyAccessor.Accessibility != null)
			{
				propertyBodyBlock.Append($"{propertyAccessor.Accessibility.Value.ToCodeString()} ");
			}

			propertyBodyBlock.Append(accessorName);
			propertyAccessor.Impl.Match(
				() => propertyBodyBlock.AppendLine(";"),
				bodyWriter =>
				{
					propertyBodyBlock.AppendLine();
					using var accessorBodyBlock = propertyBodyBlock.NewBlock();
					bodyWriter(propertyDefinition, accessorBodyBlock);
				});
		}
	}

	private static void WriteConstructor(ConstructorDefinition constructorDefinition, TypeDefinition typeDefinition,
		CodeWriter typeBodyBlock)
	{
		WriteConstructorDeclaration(constructorDefinition, typeDefinition, typeBodyBlock);
		using var constructorBodyBlock = typeBodyBlock.NewBlock();
		constructorDefinition.BodyWriter(constructorDefinition, constructorBodyBlock);
	}

	private static void WriteConstructorDeclaration(ConstructorDefinition constructorDefinition, TypeDefinition typeDefinition,
		CodeWriter typeBodyBlock)
	{
		var declarationStr = new DeclarationBuilder()
			.AddIf(constructorDefinition.Accessibility != null, () => constructorDefinition.Accessibility!.Value.ToCodeString())
			.AddIf(constructorDefinition.IsStatic, () => "static")
			.Add(typeDefinition.NameWithoutGenerics)
			.ToString();

		typeBodyBlock.AppendLine($"{declarationStr}({ToParametersString(constructorDefinition.Parameters)})");
	}

	private static void WriteMethod(MethodDefinition methodDefinition, CodeWriter typeBodyBlock)
	{
		WriteMethodDeclaration(methodDefinition, typeBodyBlock);
		if (methodDefinition.BodyWriter is not { } bodyWriter)
		{
			return;
		}

		using var methodBodyBlock = typeBodyBlock.NewBlock();
		bodyWriter(methodDefinition, methodBodyBlock);
	}

	private static void WriteMethodDeclaration(MethodDefinition methodDefinition, CodeWriter typeBodyBlock)
	{
		var declarationStr = new DeclarationBuilder()
			.AddIf(methodDefinition.Accessibility != null, () => methodDefinition.Accessibility!.Value.ToCodeString())
			.AddIf(
				methodDefinition.MethodModifier != null,
				() => methodDefinition.MethodModifier!.Match(() => "static", () => "abstract", () => "virtual", () => "override"))
			.AddIf(methodDefinition.IsPartial, () => "partial")
			.Add(methodDefinition.ReturnType.FullyQualifiedName)
			.Add(methodDefinition.Name)
			.ToString();

		var genericParametersStr = methodDefinition.GenericParameters.Count > 0
			? $"<{string.Join(", ", methodDefinition.GenericParameters)}>"
			: string.Empty;
		var parametersStr = ToParametersString(methodDefinition.Parameters);
		var endSemicolon = methodDefinition.BodyWriter == null ? ";" : string.Empty;

		typeBodyBlock.AppendLine($"{declarationStr}{genericParametersStr}({parametersStr}){endSemicolon}");
	}

	private static void WriteOperator(OperatorDefinition operatorDefinition, CodeWriter typeBodyBlock)
	{
		typeBodyBlock.AppendLine(
			$"public static {operatorDefinition.ReturnType} operator {operatorDefinition.Name}({ToParametersString(operatorDefinition.Parameters)})");

		using var methodBodyBlock = typeBodyBlock.NewBlock();
		operatorDefinition.BodyWriter(operatorDefinition, methodBodyBlock);
	}

	private static void WriteAttribute(string attribute, CodeWriter codeWriter)
	{
		codeWriter.Append("[").Append(attribute).AppendLine("]");
	}

	private static string ToParametersString(IReadOnlyList<MethodParameter> methodParameters) =>
		string.Join(", ", methodParameters.Select(x =>
		{
			var modifierStr = x.Modifier == null
				? string.Empty
				: $"{x.Modifier.Value.Match(() => "in", () => "ref", () => "out")} ";
			return $"{modifierStr}{x.TypeName} {x.Name}";
		}));

	private sealed class DeclarationBuilder
	{
		private readonly List<string> _parts = new(4);

		public DeclarationBuilder AddIf(bool condition, Func<string> valueProvider)
		{
			if (condition)
			{
				_parts.Add(valueProvider());
			}

			return this;
		}

		public DeclarationBuilder AddIf(bool condition, params Func<string>[] valueProviders)
		{
			if (condition)
			{
				foreach (var valueProvider in valueProviders)
				{
					_parts.Add(valueProvider());
				}
			}

			return this;
		}

		public DeclarationBuilder Add(string value)
		{
			_parts.Add(value);
			return this;
		}

		public override string ToString() => string.Join(" ", _parts);
	}
}