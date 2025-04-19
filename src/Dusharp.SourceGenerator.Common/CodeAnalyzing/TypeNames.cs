namespace Dusharp.SourceGenerator.Common.CodeAnalyzing;

public static class TypeNames
{
	public static readonly TypeName Void = new(TypeInfos.Void, false);
	public static readonly TypeName Boolean = new(TypeInfos.Boolean, false);
	public static readonly TypeName Int32 = new(TypeInfos.Int32, false);
	public static readonly TypeName Byte = new(TypeInfos.Byte, false);

	public static readonly TypeName Utf8JsonReader = new(TypeInfos.Utf8JsonReader, false);
	public static readonly TypeName JsonEncodedText = new(TypeInfos.JsonEncodedText, false);
	public static readonly TypeName JsonTokenType = new(TypeInfos.JsonTokenType, false);

	public static readonly TypeName JsonEncodedValue = new(TypeInfos.JsonEncodedValue, false);

	public static TypeName Object(bool isRefNullable = false) => new(TypeInfos.Object, isRefNullable);

	public static TypeName String(bool isRefNullable = false) => new(TypeInfos.String, isRefNullable);

	public static TypeName Type(bool isRefNullable = false) => new(TypeInfos.Type, isRefNullable);

	public static TypeName Utf8JsonWriter(bool isRefNullable = false) => new(TypeInfos.Utf8JsonWriter, isRefNullable);

	public static TypeName JsonSerializerOptions(bool isRefNullable = false) => new(TypeInfos.JsonSerializerOptions, isRefNullable);
}