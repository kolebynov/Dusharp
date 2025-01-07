using Dusharp.Json;

namespace Dusharp.Benchmarks
{
	[Union]
	[GenerateJsonConverter]
	public partial class TestUnion<T>
	{
		[UnionCase]
		public static partial TestUnion<T> Case1();

		[UnionCase]
		public static partial TestUnion<T> Case2(string value1, int value2);

		[UnionCase]
		public static partial TestUnion<T> Case3(string value);

		[UnionCase]
		public static partial TestUnion<T> Case4(T value1, string value2, long value3, DateTime value4);
	}
}