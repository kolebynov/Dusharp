using Dusharp;

namespace TestUnion;

[Union]
public partial struct StructUnion<T1, T2, T3>
	where T1 : unmanaged
	where T2 : class
{
	[UnionCase]
	public static partial StructUnion<T1, T2, T3> Case1();

	[UnionCase]
	public static partial StructUnion<T1, T2, T3> Case2(int value1, T1 value2, T2 value3, T3 value4);

	[UnionCase]
	public static partial StructUnion<T1, T2, T3> Case3(long? value1, T1? value2, T2? value3, T3? value4, T1 value5);
}