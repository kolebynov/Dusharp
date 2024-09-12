namespace Dusharp.Tests
{
	[Union]
#pragma warning disable CS0660, CS0661 // Equals overriden in derived classes.
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