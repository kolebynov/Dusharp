using Dusharp.Json;

namespace Dusharp.Tests
{
	[Union]
	[GenerateJsonConverter]
	public partial struct TestStructUnion<T>
	{
		[UnionCase]
		public static partial TestStructUnion<T> Case1();

		[UnionCase]
		public static partial TestStructUnion<T> Case2(string value1, int value2);

		[UnionCase]
		public static partial TestStructUnion<T> Case3(string value);

		[UnionCase]
		public static partial TestStructUnion<T> Case4(T value);
	}
}