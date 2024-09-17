namespace Dusharp.CodeGeneration;

[Union]
public partial class TypeKind
{
	[UnionCase]
	public static partial TypeKind Class(bool isAbstract, bool isSealed);

	[UnionCase]
	public static partial TypeKind Struct(bool isReadOnly);
}