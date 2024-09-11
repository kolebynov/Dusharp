using FluentAssertions;
using Xunit;

namespace Dusharp.Tests
{
	public class EqualityTests
	{
		[Fact]
		public void New_ForCaseWithoutParameters_ReturnTheSameInstance()
		{
			// Arrange and Act

			var union1 = TestUnion.Case1();
			var union2 = TestUnion.Case1();

			// Assert

			ReferenceEquals(union1, union2).Should().BeTrue();
		}

		[Fact]
		public void Equals_ForSameInstance_ReturnTrue()
		{
			// Arrange

			var union1 = TestUnion.Case2("value", 1);

			// Act and Assert

			union1.Equals(union1).Should().BeTrue();
		}

		[Fact]
		public void Equals_ForSameCasesWithSameValues_ReturnTrue()
		{
			// Arrange

			var union1 = TestUnion.Case2("value", 1);
			var union2 = TestUnion.Case2("value", 1);

			// Act and Assert

			union1.Equals(union2).Should().BeTrue();
		}

		[Fact]
		public void Equals_ForSameCasesWithDifferentValues_ReturnFalse()
		{
			// Arrange

			var union1 = TestUnion.Case2("value", 1);
			var union2 = TestUnion.Case2("value", 2);
			var union3 = TestUnion.Case2("value1", 1);

			// Act and Assert

			union1.Equals(union2).Should().BeFalse();
			union2.Equals(union3).Should().BeFalse();
			union1.Equals(union3).Should().BeFalse();
		}

		[Fact]
		public void Equals_ForDiffCases_ReturnFalse()
		{
			// Arrange

			var union1 = TestUnion.Case2("value", 1);
			var union2 = TestUnion.Case3("value");

			// Act and Assert

			union1.Equals(union2).Should().BeFalse();
		}
	}
}