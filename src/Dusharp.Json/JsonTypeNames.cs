using Dusharp.SourceGenerator.Common.CodeAnalyzing;

namespace Dusharp.Json;

public static class JsonTypeNames
{
	public static readonly TypeName Utf8JsonReader = new(JsonTypeInfos.Utf8JsonReader, false);
	public static readonly TypeName JsonEncodedText = new(JsonTypeInfos.JsonEncodedText, false);
	public static readonly TypeName JsonTokenType = new(JsonTypeInfos.JsonTokenType, false);

	public static readonly TypeName JsonEncodedValue = new(JsonTypeInfos.JsonEncodedValue, false);

	public static TypeName Utf8JsonWriter(bool isRefNullable = false) => new(JsonTypeInfos.Utf8JsonWriter, isRefNullable);

	public static TypeName JsonSerializerOptions(bool isRefNullable = false) => new(JsonTypeInfos.JsonSerializerOptions, isRefNullable);
}