namespace Dusharp.Tests
{
	public static partial class TestNestedUnion
	{
		[Union]
		public partial class TestUnion<T>
		{
			[UnionCase]
			public static partial TestUnion<T> Case1();

			[UnionCase]
			public static partial TestUnion<T> Case2(string value1, int value2);

			[UnionCase]
			public static partial TestUnion<T> Case3(string value);

			[UnionCase]
			public static partial TestUnion<T> Case4(T value);
		}
	}
}