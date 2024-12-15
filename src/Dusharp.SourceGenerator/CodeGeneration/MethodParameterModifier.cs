namespace Dusharp.CodeGeneration;

[Union]
public partial struct MethodParameterModifier
{
	[UnionCase]
	public static partial MethodParameterModifier In();

	[UnionCase]
	public static partial MethodParameterModifier Ref();

	[UnionCase]
	public static partial MethodParameterModifier Out();
}