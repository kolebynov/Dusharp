namespace Dusharp.Tests
{
	[Union]
	public partial class TestUnion
	{
		[UnionCase]
		public static partial TestUnion Case1();

		[UnionCase]
		public static partial TestUnion Case2(string value1, int value2);

		[UnionCase]
		public static partial TestUnion Case3(string value);
	}
}