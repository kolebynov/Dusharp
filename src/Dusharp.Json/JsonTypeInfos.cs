using Dusharp.SourceGenerator.Common.CodeAnalyzing;

namespace Dusharp.Json;

public static class JsonTypeInfos
{
	private const string JsonNs = "System.Text.Json";
	private const string JsonSerializationNs = $"{JsonNs}.Serialization";
	private const string DusharpJsonNs = $"{TypeInfos.DusharpNs}.Json";

	public static readonly TypeInfo Utf8JsonReader = TypeInfo.SpecificType(JsonNs, null, "Utf8JsonReader", new TypeInfo.TypeKind.ValueType(false));
	public static readonly TypeInfo Utf8JsonWriter = TypeInfo.SpecificType(JsonNs, null, "Utf8JsonWriter", new TypeInfo.TypeKind.ReferenceType(false));
	public static readonly TypeInfo JsonSerializerOptions = TypeInfo.SpecificType(JsonNs, null, "JsonSerializerOptions", new TypeInfo.TypeKind.ReferenceType(false));
	public static readonly TypeInfo JsonEncodedText = TypeInfo.SpecificType(JsonNs, null, "JsonEncodedText", new TypeInfo.TypeKind.ValueType(false));
	public static readonly TypeInfo JsonTokenType = TypeInfo.SpecificType(JsonNs, null, "JsonTokenType", new TypeInfo.TypeKind.ValueType(true));

	public static readonly TypeInfo JsonEncodedValue = TypeInfo.SpecificType(DusharpJsonNs, null, "JsonEncodedValue", new TypeInfo.TypeKind.ValueType(false));
	public static readonly TypeInfo JsonConverterHelpers = TypeInfo.SpecificType(DusharpJsonNs, null, "JsonConverterHelpers", new TypeInfo.TypeKind.ReferenceType(false));

	public static TypeInfo JsonConverter(TypeName arg) =>
		TypeInfo.SpecificType(JsonSerializationNs, null, $"JsonConverter<{arg.FullyQualifiedName}>",
			new TypeInfo.TypeKind.ReferenceType(false));
}