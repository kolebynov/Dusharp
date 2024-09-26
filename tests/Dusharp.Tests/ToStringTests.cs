using FluentAssertions;
using Xunit;

namespace Dusharp.Tests
{
	public class ToStringTests
	{
		[Fact]
		public void ToString_ForAnyCase_ReturnCorrectString()
		{
			// Arrange

			var union1 = TestUnion<long>.Case1();
			var union2 = TestUnion<long>.Case2("value", 2);
			var union3 = TestUnion<long>.Case3("value");
			var union4 = TestUnion<long>.Case4(10);

			var structUnion1 = TestStructUnion<long>.Case1();
			var structUnion2 = TestStructUnion<long>.Case2("value", 2);
			var structUnion3 = TestStructUnion<long>.Case3("value");
			var structUnion4 = TestStructUnion<long>.Case4(10);

			// Act and Assert

			union1.ToString().Should().Be("Case1");
			union2.ToString().Should().Be("Case2 { value1 = value, value2 = 2 }");
			union3.ToString().Should().Be("Case3 { value = value }");
			union4.ToString().Should().Be("Case4 { value = 10 }");

			structUnion1.ToString().Should().Be("Case1");
			structUnion2.ToString().Should().Be("Case2 { value1 = value, value2 = 2 }");
			structUnion3.ToString().Should().Be("Case3 { value = value }");
			structUnion4.ToString().Should().Be("Case4 { value = 10 }");
		}
	}
}