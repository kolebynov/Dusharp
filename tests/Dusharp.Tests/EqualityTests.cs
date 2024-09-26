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

			var union1 = TestUnion<long>.Case1();
			var union2 = TestUnion<long>.Case1();

			// Assert

			ReferenceEquals(union1, union2).Should().BeTrue();
		}

		[Fact]
		public void Equals_ForSameInstance_ReturnTrue()
		{
			// Arrange

			var union1 = TestUnion<long>.Case2("value", 1);

			var structUnion1 = TestStructUnion<long>.Case2("value", 1);

			// Act and Assert

			union1.Equals(union1).Should().BeTrue();
			structUnion1.Equals(structUnion1).Should().BeTrue();
		}

		[Fact]
		public void Equals_ForSameCasesWithSameValues_ReturnTrue()
		{
			// Arrange

			var union1 = TestUnion<long>.Case2("value", 1);
			var union2 = TestUnion<long>.Case2("value", 1);
			var union3 = TestUnion<long>.Case4(10);
			var union4 = TestUnion<long>.Case4(10);

			var structUnion1 = TestStructUnion<long>.Case2("value", 1);
			var structUnion2 = TestStructUnion<long>.Case2("value", 1);
			var structUnion3 = TestStructUnion<long>.Case4(10);
			var structUnion4 = TestStructUnion<long>.Case4(10);

			// Act and Assert

			union1.Equals(union2).Should().BeTrue();
			union3.Equals(union4).Should().BeTrue();

			structUnion1.Equals(structUnion2).Should().BeTrue();
			structUnion3.Equals(structUnion4).Should().BeTrue();
		}

		[Fact]
		public void Equals_ForSameCasesWithDifferentValues_ReturnFalse()
		{
			// Arrange

			var union1 = TestUnion<long>.Case2("value", 1);
			var union2 = TestUnion<long>.Case2("value", 2);
			var union3 = TestUnion<long>.Case2("value1", 1);

			var structUnion1 = TestStructUnion<long>.Case2("value", 1);
			var structUnion2 = TestStructUnion<long>.Case2("value", 2);
			var structUnion3 = TestStructUnion<long>.Case2("value1", 1);

			// Act and Assert

			union1.Equals(union2).Should().BeFalse();
			union2.Equals(union3).Should().BeFalse();
			union1.Equals(union3).Should().BeFalse();

			structUnion1.Equals(structUnion2).Should().BeFalse();
			structUnion2.Equals(structUnion3).Should().BeFalse();
			structUnion1.Equals(structUnion3).Should().BeFalse();
		}

		[Fact]
		public void Equals_ForDiffCases_ReturnFalse()
		{
			// Arrange

			var union1 = TestUnion<long>.Case2("value", 1);
			var union2 = TestUnion<long>.Case3("value");

			var structUnion1 = TestStructUnion<long>.Case2("value", 1);
			var structUnion2 = TestStructUnion<long>.Case3("value");

			// Act and Assert

			union1.Equals(union2).Should().BeFalse();
			structUnion1.Equals(structUnion2).Should().BeFalse();
		}

		[Fact]
		public void Equals_ForComparingWithNull_ReturnFalse()
		{
			// Arrange

			var union1 = TestUnion<long>.Case2("value", 1);

			var structUnion1 = TestStructUnion<long>.Case2("value", 1);

			// Act and Assert

			union1.Equals(null).Should().BeFalse();
			structUnion1.Equals(null).Should().BeFalse();
		}

		[Fact]
		public void EqualityOp_ForAnyCases_ReturnCorrectResult()
		{
			// Arrange

			var union1 = TestUnion<long>.Case2("value", 1);
			var union2 = TestUnion<long>.Case2("value", 1);
			var union3 = TestUnion<long>.Case2("value", 2);
			TestUnion<long>? nullUnion1 = null;
			TestUnion<long>? nullUnion2 = null;

			var structUnion1 = TestStructUnion<long>.Case2("value", 1);
			var structUnion2 = TestStructUnion<long>.Case2("value", 1);
			var structUnion3 = TestStructUnion<long>.Case2("value", 2);

			// Act and Assert

			(union1 == union2).Should().BeTrue();
			(union1 == union3).Should().BeFalse();
			(union1 == nullUnion1).Should().BeFalse();
#pragma warning disable CA1508
			(nullUnion1 == nullUnion2).Should().BeTrue();
#pragma warning restore CA1508

			(structUnion1 == structUnion2).Should().BeTrue();
			(structUnion1 == structUnion3).Should().BeFalse();
		}

		[Fact]
		public void InequalityOp_ForAnyCases_ReturnCorrectResult()
		{
			// Arrange

			var union1 = TestUnion<long>.Case2("value", 1);
			var union2 = TestUnion<long>.Case2("value", 1);
			var union3 = TestUnion<long>.Case2("value", 2);
			TestUnion<long>? nullUnion1 = null;
			TestUnion<long>? nullUnion2 = null;

			var structUnion1 = TestStructUnion<long>.Case2("value", 1);
			var structUnion2 = TestStructUnion<long>.Case2("value", 1);
			var structUnion3 = TestStructUnion<long>.Case2("value", 2);

			// Act and Assert

			(union1 != union2).Should().BeFalse();
			(union1 != union3).Should().BeTrue();
			(union1 != nullUnion1).Should().BeTrue();
#pragma warning disable CA1508
			(nullUnion1 != nullUnion2).Should().BeFalse();
#pragma warning restore CA1508

			(structUnion1 != structUnion2).Should().BeFalse();
			(structUnion1 != structUnion3).Should().BeTrue();
		}

		[Fact]
		public void GetHashCode_ForSameInstance_ReturnSameHashCode()
		{
			// Arrange

			var union1 = TestUnion<long>.Case2("value", 1);

			var structUnion1 = TestStructUnion<long>.Case2("value", 1);

			// Act and Assert

			union1.GetHashCode().Equals(union1.GetHashCode()).Should().BeTrue();
			structUnion1.GetHashCode().Equals(structUnion1.GetHashCode()).Should().BeTrue();
		}

		[Fact]
		public void GetHashCode_ForSameCasesWithSameValues_ReturnSameHashCode()
		{
			// Arrange

			var union1 = TestUnion<long>.Case2("value", 1);
			var union2 = TestUnion<long>.Case2("value", 1);
			var union3 = TestUnion<long>.Case4(10);
			var union4 = TestUnion<long>.Case4(10);

			var structUnion1 = TestStructUnion<long>.Case2("value", 1);
			var structUnion2 = TestStructUnion<long>.Case2("value", 1);
			var structUnion3 = TestStructUnion<long>.Case4(10);
			var structUnion4 = TestStructUnion<long>.Case4(10);

			// Act and Assert

			union1.GetHashCode().Equals(union2.GetHashCode()).Should().BeTrue();
			union3.GetHashCode().Equals(union4.GetHashCode()).Should().BeTrue();

			structUnion1.GetHashCode().Equals(structUnion2.GetHashCode()).Should().BeTrue();
			structUnion3.GetHashCode().Equals(structUnion4.GetHashCode()).Should().BeTrue();
		}

		[Fact]
		public void GetHashCode_ForSameCasesWithDifferentValues_ReturnDifferentHashCode()
		{
			// Arrange

			var union1 = TestUnion<long>.Case2("value", 1);
			var union2 = TestUnion<long>.Case2("value", 2);
			var union3 = TestUnion<long>.Case2("value1", 1);

			var structUnion1 = TestStructUnion<long>.Case2("value", 1);
			var structUnion2 = TestStructUnion<long>.Case2("value", 2);
			var structUnion3 = TestStructUnion<long>.Case2("value1", 1);

			// Act and Assert

			union1.GetHashCode().Equals(union2.GetHashCode()).Should().BeFalse();
			union2.GetHashCode().Equals(union3.GetHashCode()).Should().BeFalse();
			union1.GetHashCode().Equals(union3.GetHashCode()).Should().BeFalse();

			structUnion1.GetHashCode().Equals(structUnion2.GetHashCode()).Should().BeFalse();
			structUnion2.GetHashCode().Equals(structUnion3.GetHashCode()).Should().BeFalse();
			structUnion1.GetHashCode().Equals(structUnion3.GetHashCode()).Should().BeFalse();
		}

		[Fact]
		public void GetHashCode_ForDiffCases_ReturnDifferentHashCode()
		{
			// Arrange

			var union1 = TestUnion<long>.Case2("value", 1);
			var union2 = TestUnion<long>.Case3("value");

			var structUnion1 = TestStructUnion<long>.Case2("value", 1);
			var structUnion2 = TestStructUnion<long>.Case3("value");

			// Act and Assert

			structUnion1.GetHashCode().Equals(structUnion2.GetHashCode()).Should().BeFalse();
			structUnion1.GetHashCode().Equals(structUnion2.GetHashCode()).Should().BeFalse();
		}
	}
}