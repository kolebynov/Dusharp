using System;
using FluentAssertions;
using Xunit;

namespace Dusharp.Tests
{
	public class MatchTests
	{
		[Fact]
		public void Match_IfOneOfHandlerIsNull_ThrowsArgumentNullException()
		{
			// Arrange

			var union = TestUnion.Case2("test", 2);

			// Act and Assert

			union.Invoking(x => x.Match(null, (_, _) => { }, _ => { })).Should().Throw<ArgumentNullException>();
			union.Invoking(x => x.Match(() => { }, null, _ => { })).Should().Throw<ArgumentNullException>();
			union.Invoking(x => x.Match(() => { }, (_, _) => { }, null)).Should().Throw<ArgumentNullException>();
		}

		[Fact]
		public void Match_ForMatchWithoutStateWithoutReturn_InvokeCorrectHandler()
		{
			// Arrange

			var union1 = TestUnion.Case1();
			var union2 = TestUnion.Case2("value", 2);
			var union3 = TestUnion.Case3("value");

			// Act and Assert

			union1.Match(
				() => { },
				(_, _) => Assert.Fail("Invalid handler invoked"),
				_ => Assert.Fail("Invalid handler invoked"));
			union2.Match(
				() => Assert.Fail("Invalid handler invoked"),
				(v1, v2) =>
				{
					v1.Should().Be("value");
					v2.Should().Be(2);
				},
				_ => Assert.Fail("Invalid handler invoked"));
			union3.Match(
				() => Assert.Fail("Invalid handler invoked"),
				(_, _) => Assert.Fail("Invalid handler invoked"),
				v1 => v1.Should().Be("value"));
		}

		[Fact]
		public void Match_ForMatchWithoutStateWithReturn_InvokeCorrectHandler()
		{
			// Arrange

			var union1 = TestNestedUnion.TestUnion.Case1();
			var union2 = TestNestedUnion.TestUnion.Case2("value", 2);
			var union3 = TestNestedUnion.TestUnion.Case3("value");

			// Act and Assert

			union1.Match(() => "0", (v1, v2) => $"{v1} {v2}", v1 => v1).Should().Be("0", "Invalid handler invoked");
			union2.Match(() => "0", (v1, v2) => $"{v1} {v2}", v1 => v1).Should().Be("value 2", "Invalid handler invoked");
			union3.Match(() => "0", (v1, v2) => $"{v1} {v2}", v1 => v1).Should().Be("value", "Invalid handler invoked");
		}

		[Fact]
		public void Match_ForMatchWithStateWithoutReturn_InvokeCorrectHandler()
		{
			// Arrange

			var union1 = TestUnion.Case1();
			var union2 = TestUnion.Case2("value", 2);
			var union3 = TestUnion.Case3("value");

			// Act and Assert

			union1.Match(
				"0",
				st => st.Should().Be("0"),
				(_, _, _) => Assert.Fail("Invalid handler invoked"),
				(_, _) => Assert.Fail("Invalid handler invoked"));
			union2.Match(
				"0",
				_ => Assert.Fail("Invalid handler invoked"),
				(st, v1, v2) =>
				{
					st.Should().Be("0");
					v1.Should().Be("value");
					v2.Should().Be(2);
				},
				(_, _) => Assert.Fail("Invalid handler invoked"));
			union3.Match(
				"0",
				_ => Assert.Fail("Invalid handler invoked"),
				(_, _, _) => Assert.Fail("Invalid handler invoked"),
				(st, v1) =>
				{
					st.Should().Be("0");
					v1.Should().Be("value");
				});
		}

		[Fact]
		public void Match_ForMatchWithStateWithReturn_InvokeCorrectHandler()
		{
			// Arrange

			var union1 = TestNestedUnion.TestUnion.Case1();
			var union2 = TestNestedUnion.TestUnion.Case2("value", 2);
			var union3 = TestNestedUnion.TestUnion.Case3("value");

			// Act and Assert

			union1.Match("0", st => st, (st, v1, v2) => $"{st} {v1} {v2}", (st, v1) => $"{st} {v1}")
				.Should().Be("0", "Invalid handler invoked");
			union2.Match("0", st => st, (st, v1, v2) => $"{st} {v1} {v2}", (st, v1) => $"{st} {v1}")
				.Should().Be("0 value 2", "Invalid handler invoked");
			union3.Match("0", st => st, (st, v1, v2) => $"{st} {v1} {v2}", (st, v1) => $"{st} {v1}")
				.Should().Be("0 value", "Invalid handler invoked");
		}
	}
}