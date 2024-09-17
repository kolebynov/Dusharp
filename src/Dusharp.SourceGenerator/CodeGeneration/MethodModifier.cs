namespace Dusharp.CodeGeneration;

[Union]
public partial class MethodModifier
{
	[UnionCase]
	public static partial MethodModifier Static();

	[UnionCase]
	public static partial MethodModifier Abstract();

	[UnionCase]
	public static partial MethodModifier Virtual();

	[UnionCase]
	public static partial MethodModifier Override();
}