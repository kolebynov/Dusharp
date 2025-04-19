using System.Text;
using Dusharp.SourceGenerator.Common;
using Dusharp.SourceGenerator.Common.CodeAnalyzing;
using Dusharp.SourceGenerator.Common.CodeGeneration;
using Dusharp.SourceGenerator.Common.Extensions;
using Microsoft.CodeAnalysis;
using TypeKind = Dusharp.SourceGenerator.Common.CodeGeneration.TypeKind;

namespace Dusharp.Json;

public sealed class JsonConverterGenerator : IUnionCodeGenerator
{
	private const string UnionTypeFieldName = "UnionType";

	private readonly TypeCodeWriter _typeCodeWriter;

	public JsonConverterGenerator(TypeCodeWriter typeCodeWriter)
	{
		_typeCodeWriter = typeCodeWriter;
	}

	public string Name => "Json";

	public string? GenerateCode(UnionInfo unionInfo, INamedTypeSymbol unionTypeSymbol)
	{
		if (unionTypeSymbol.GetAttributes()
		    .All(x => x.AttributeClass!.ToString() != "Dusharp.Json.GenerateJsonConverterAttribute"))
		{
			return null;
		}

		using var codeWriter = CodeWriter.CreateWithDefaultLines();

		CodeWritingUtils.WriteContainingBlocks(
			unionInfo.TypeInfo, codeWriter,
			innerBlock =>
			{
				var jsonConverterTypeDefinition = new TypeDefinition
				{
					Kind = TypeKind.Class(false, true),
					Name = "JsonConverter",
					Accessibility = Accessibility.Public,
					InheritedTypes = [TypeInfos.JsonConverter(new TypeName(unionInfo.TypeInfo, false))],
					Fields =
					[
						new FieldDefinition
						{
							Name = UnionTypeFieldName,
							TypeName = TypeNames.Type(),
							Accessibility = Accessibility.Private,
							IsStatic = true,
							IsReadOnly = true,
							Initializer = $"typeof({unionInfo.TypeInfo})",
						},
						..GetEncodedNameFields(unionInfo),
					],
					Methods =
					[
						new MethodDefinition
						{
							Name = "CanConvert",
							ReturnType = TypeNames.Boolean,
							Accessibility = Accessibility.Public,
							MethodModifier = MethodModifier.Override(),
							Parameters = [new MethodParameter(TypeNames.Type(), "typeToConvert")],
							BodyWriter = GetCanConvertMethodBodyWriter(unionInfo),
						},
						new MethodDefinition
						{
							Name = "Read",
							ReturnType = new TypeName(unionInfo.TypeInfo, true),
							Accessibility = Accessibility.Public,
							MethodModifier = MethodModifier.Override(),
							Parameters =
							[
								new MethodParameter(TypeNames.Utf8JsonReader, "reader", MethodParameterModifier.Ref()),
								new MethodParameter(TypeNames.Type(), "typeToConvert"),
								new MethodParameter(TypeNames.JsonSerializerOptions(), "options"),
							],
							BodyWriter = GetReadMethodBodyWriter(),
						},
						new MethodDefinition
						{
							Name = "Write",
							ReturnType = TypeNames.Void,
							Accessibility = Accessibility.Public,
							MethodModifier = MethodModifier.Override(),
							Parameters =
							[
								new MethodParameter(TypeNames.Utf8JsonWriter(), "writer"),
								new MethodParameter(new TypeName(unionInfo.TypeInfo, false), "value"),
								new MethodParameter(TypeNames.JsonSerializerOptions(), "options"),
							],
							BodyWriter = GetWriteMethodBodyWriter(unionInfo),
						},
						new MethodDefinition
						{
							Name = "Deserialize",
							ReturnType = new TypeName(unionInfo.TypeInfo, false),
							Accessibility = Accessibility.Private,
							MethodModifier = MethodModifier.Static(),
							Parameters =
							[
								new MethodParameter(TypeNames.Utf8JsonReader, "reader", MethodParameterModifier.Ref()),
								new MethodParameter(TypeNames.JsonSerializerOptions(), "options"),
							],
							BodyWriter = GetDeserializeMethodBodyWriter(unionInfo),
						},
					],
				};

				var unionTypeDefinition = new TypeDefinition
				{
					Kind = unionInfo.TypeInfo.Kind.Match(
						_ => TypeKind.Class(false, false),
						_ => TypeKind.Struct(false),
						() => throw new ArgumentException("Unknown union type kind", nameof(unionInfo))),
					Name = unionInfo.TypeInfo.Name,
					IsPartial = true,
					NestedTypes = [jsonConverterTypeDefinition],
				};

				_typeCodeWriter.WriteType(unionTypeDefinition, innerBlock);
			});

		return codeWriter.ToString();
	}

	private static IEnumerable<FieldDefinition> GetEncodedNameFields(UnionInfo union) =>
		union.Cases
			.SelectMany(unionCase => unionCase.Parameters
				.Select(p => GetEncodedValueField(GetUnionCaseParameterEncodedValueFieldName(unionCase.Name, p.Name), p.Name))
				.Prepend(GetEncodedValueField(GetUnionCaseEncodedValueFieldName(unionCase.Name), unionCase.Name)));

	private static Action<MethodDefinition, CodeWriter> GetCanConvertMethodBodyWriter(UnionInfo union) =>
		(def, methodBodyBlock) =>
		{
			var typeToConvertParameter = def.Parameters[0].Name;
			methodBodyBlock
				.Append($"return {typeToConvertParameter} == {UnionTypeFieldName}")
				.AppendLine(union.TypeInfo.Kind.IsReferenceType
					? $" || {typeToConvertParameter}.BaseType == {UnionTypeFieldName};"
					: ";");
		};

	private static Action<MethodDefinition, CodeWriter> GetReadMethodBodyWriter() =>
		(def, methodBodyBlock) =>
		{
			var readerName = def.Parameters[0].Name;
			var optionsName = def.Parameters[2].Name;

			methodBodyBlock.AppendLine($"{TypeInfos.JsonConverterHelpers}.BeforeRead(ref {readerName}, {UnionTypeFieldName});");
			methodBodyBlock.AppendLine($"var value = Deserialize(ref {readerName}, {optionsName});");
			methodBodyBlock.AppendLine($"{TypeInfos.JsonConverterHelpers}.AfterRead(ref {readerName}, {UnionTypeFieldName});");
			methodBodyBlock.AppendLine("return value;");
		};

	private static Action<MethodDefinition, CodeWriter> GetWriteMethodBodyWriter(UnionInfo union) =>
		(def, methodBodyBlock) =>
		{
			var writerName = def.Parameters[0].Name;
			var valueName = def.Parameters[1].Name;
			var optionsName = def.Parameters[2].Name;

			foreach (var unionCase in union.Cases)
			{
				var parameterVariableNames = unionCase.Parameters.Select(x => $"{unionCase.Name}_{x.Name}").ToArray();
				var parametersStr = string.Join(", ", parameterVariableNames.Select(x => $"out var {x}"));
				methodBodyBlock.AppendLine(
					$"if ({valueName}.{UnionNamesProvider.GetTryGetCaseDataMethodName(unionCase.Name)}({parametersStr}))");
				using var thenBlock = methodBodyBlock.NewBlock();
				if (unionCase.HasParameters)
				{
					thenBlock.AppendLine($"{writerName}.WriteStartObject();");
					thenBlock.AppendLine($"{writerName}.WriteStartObject({GetUnionCaseEncodedValueFieldName(unionCase.Name)}.EncodedValue);");
					foreach (var (unionCaseParameter, name) in unionCase.Parameters.Zip(parameterVariableNames, (x, y) => (x, y)))
					{
						thenBlock.AppendLine($"{TypeInfos.JsonConverterHelpers}.WriteProperty({writerName}, {GetUnionCaseParameterEncodedValueFieldName(unionCase.Name, unionCaseParameter.Name)}.EncodedValue, {name}, {optionsName});");
					}

					thenBlock.AppendLine($"{writerName}.WriteEndObject();");
					thenBlock.AppendLine($"{writerName}.WriteEndObject();");
				}
				else
				{
					thenBlock.AppendLine($"{writerName}.WriteStringValue({GetUnionCaseEncodedValueFieldName(unionCase.Name)}.EncodedValue);");
				}

				thenBlock.AppendLine("return;");
			}

			methodBodyBlock.AppendLine($"""{TypeInfos.JsonConverterHelpers}.ThrowUnionInInvalidState({UnionTypeFieldName}, "{valueName}");""");
		};

	private static Action<MethodDefinition, CodeWriter> GetDeserializeMethodBodyWriter(UnionInfo union) =>
		(def, methodBodyBlock) =>
		{
			var readerName = def.Parameters[0].Name;
			var optionsName = def.Parameters[1].Name;

			methodBodyBlock.AppendLine($"if ({readerName}.TokenType == {TypeNames.JsonTokenType.FullyQualifiedName}.String)");
			using (var parameterlessUnionBlock = methodBodyBlock.NewBlock())
			{
				foreach (var parameterlessUnionCase in union.Cases.Where(x => !x.HasParameters))
				{
					parameterlessUnionBlock.AppendLine($"if ({readerName}.ValueTextEquals({GetUnionCaseEncodedValueFieldName(parameterlessUnionCase.Name)}.Utf8Value))");
					using var thenBlock = parameterlessUnionBlock.NewBlock();
					thenBlock.AppendLine($"return {union.TypeInfo}.{parameterlessUnionCase.Name}();");
				}

				parameterlessUnionBlock.AppendLine($"{TypeInfos.JsonConverterHelpers}.ThrowInvalidParameterlessCaseName(ref {readerName}, {UnionTypeFieldName});");
			}

			methodBodyBlock
				.Append($"if (!{TypeInfos.JsonConverterHelpers}.ReadAndTokenIsPropertyName(ref {readerName}))")
				.AppendLine($" {TypeInfos.JsonConverterHelpers}.ThrowInvalidUnionJsonObject(ref {readerName});");

			foreach (var withParametersUnionCase in union.Cases.Where(x => x.HasParameters))
			{
				methodBodyBlock.AppendLine($"if ({readerName}.ValueTextEquals({GetUnionCaseEncodedValueFieldName(withParametersUnionCase.Name)}.Utf8Value))");
				using var deserializeCaseBlock = methodBodyBlock.NewBlock();
				deserializeCaseBlock
					.AppendLine($"{readerName}.Read();")
					.AppendLine("var loaded = 0;");
				var parameterVariableNames = withParametersUnionCase.Parameters
					.Select(p => $"{withParametersUnionCase.Name}_{p.Name}")
					.ToArray();
				foreach (var (unionCaseParameter, name) in withParametersUnionCase.Parameters.Zip(parameterVariableNames, (x, y) => (x, y)))
				{
					deserializeCaseBlock.AppendLine($"{unionCaseParameter.TypeName.FullyQualifiedName} {name} = default!;");
				}

				deserializeCaseBlock.AppendLine($"while ({TypeInfos.JsonConverterHelpers}.ReadAndTokenIsPropertyName(ref {readerName}))");
				using (var deserializeCaseWhileBlock = deserializeCaseBlock.NewBlock())
				{
					foreach (var (unionCaseParameter, name) in withParametersUnionCase.Parameters.Zip(parameterVariableNames, (x, y) => (x, y)))
					{
						deserializeCaseWhileBlock.AppendLine(
							$"if ({readerName}.ValueTextEquals({GetUnionCaseParameterEncodedValueFieldName(withParametersUnionCase.Name, unionCaseParameter.Name)}.Utf8Value))");
						using var readCasePropertyBlock = deserializeCaseWhileBlock.NewBlock();
						readCasePropertyBlock
							.AppendLine(
								$"{name} = {TypeInfos.JsonConverterHelpers}.Deserialize<{unionCaseParameter.TypeName.FullyQualifiedName}>(ref {readerName}, {optionsName})!;")
							.AppendLine("loaded++;")
							.AppendLine("continue;");
					}
				}

				deserializeCaseBlock
					.Append($"if (loaded < {withParametersUnionCase.Parameters.Count})")
					.AppendLine($""" {TypeInfos.JsonConverterHelpers}.ThrowNotAllCaseParametersPresent({UnionTypeFieldName}, "{withParametersUnionCase.Name}", loaded, {withParametersUnionCase.Parameters.Count});""")
					.AppendLine($"return {union.TypeInfo}.{withParametersUnionCase.Name}({string.Join(", ", parameterVariableNames)});");
			}

			methodBodyBlock
				.AppendLine($"{TypeInfos.JsonConverterHelpers}.ThrowInvalidCaseName(ref {readerName}, {UnionTypeFieldName});")
				.AppendLine("return default!;");
		};

	private static FieldDefinition GetEncodedValueField(string fieldName, string value)
	{
		var utf8Value = Encoding.UTF8.GetBytes(value);
		var utf8NewArrayString = $"new {TypeNames.Byte.FullyQualifiedName}[{utf8Value.Length}] {{ {string.Join(", ", utf8Value)} }}";

		return new FieldDefinition
		{
			Name = fieldName,
			TypeName = TypeNames.JsonEncodedValue,
			Accessibility = Accessibility.Private,
			IsStatic = true,
			IsReadOnly = true,
			Initializer =
				$"new {TypeNames.JsonEncodedValue.FullyQualifiedName}({TypeNames.JsonEncodedText.FullyQualifiedName}.Encode({utf8NewArrayString}), {utf8NewArrayString})",
		};
	}

	private static string GetUnionCaseEncodedValueFieldName(string unionCaseName) => $"{unionCaseName}_EncodedValue";

	private static string GetUnionCaseParameterEncodedValueFieldName(string unionCaseName, string parameterName) =>
		$"{unionCaseName}_{parameterName}_EncodedValue";
}