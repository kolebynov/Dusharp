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

	public static TypeName ValueTuple(params TypeName[] tupleTypes) =>
		new(TypeInfos.ValueTuple(tupleTypes), false);

	public static TypeName Span(TypeName itemType) => new(TypeInfos.Span(itemType), false);

	public static TypeName ReadOnlySpan(TypeName itemType) => new(TypeInfos.ReadOnlySpan(itemType), false);
}