namespace Dusharp.SourceGenerator.Common.CodeAnalyzing;

public static class TypeNames
{
	public static readonly TypeName Void = new(TypeInfos.Void, false);
	public static readonly TypeName Boolean = new(TypeInfos.Boolean, false);
	public static readonly TypeName Int32 = new(TypeInfos.Int32, false);
	public static readonly TypeName Byte = new(TypeInfos.Byte, false);

	public static TypeName Object(bool isRefNullable = false) => new(TypeInfos.Object, isRefNullable);

	public static TypeName String(bool isRefNullable = false) => new(TypeInfos.String, isRefNullable);

	public static TypeName Type(bool isRefNullable = false) => new(TypeInfos.Type, isRefNullable);
}