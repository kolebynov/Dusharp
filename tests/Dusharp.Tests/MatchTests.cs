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

			var union = TestUnion<long>.Case2("test", 2);

			// Act and Assert

			union.Invoking(x => x.Match(null!, (_, _) => { }, _ => { }, _ => { })).Should().Throw<ArgumentNullException>();
			union.Invoking(x => x.Match(() => { }, null!, _ => { }, _ => { })).Should().Throw<ArgumentNullException>();
			union.Invoking(x => x.Match(() => { }, (_, _) => { }, null!, _ => { })).Should().Throw<ArgumentNullException>();
			union.Invoking(x => x.Match(() => { }, (_, _) => { }, _ => { }, null!)).Should().Throw<ArgumentNullException>();
		}

		[Fact]
		public void Match_ForMatchWithoutStateWithoutReturn_InvokeCorrectHandler()
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

			union1.Match(
				() => { },
				(_, _) => Assert.Fail("Invalid handler invoked"),
				_ => Assert.Fail("Invalid handler invoked"),
				_ => Assert.Fail("Invalid handler invoked"));
			union2.Match(
				() => Assert.Fail("Invalid handler invoked"),
				(v1, v2) =>
				{
					v1.Should().Be("value");
					v2.Should().Be(2);
				},
				_ => Assert.Fail("Invalid handler invoked"),
				_ => Assert.Fail("Invalid handler invoked"));
			union3.Match(
				() => Assert.Fail("Invalid handler invoked"),
				(_, _) => Assert.Fail("Invalid handler invoked"),
				v1 => v1.Should().Be("value"),
				_ => Assert.Fail("Invalid handler invoked"));
			union4.Match(
				() => Assert.Fail("Invalid handler invoked"),
				(_, _) => Assert.Fail("Invalid handler invoked"),
				_ => Assert.Fail("Invalid handler invoked"),
				v1 => v1.Should().Be(10));

			structUnion1.Match(
				() => { },
				(_, _) => Assert.Fail("Invalid handler invoked"),
				_ => Assert.Fail("Invalid handler invoked"),
				_ => Assert.Fail("Invalid handler invoked"));
			structUnion2.Match(
				() => Assert.Fail("Invalid handler invoked"),
				(v1, v2) =>
				{
					v1.Should().Be("value");
					v2.Should().Be(2);
				},
				_ => Assert.Fail("Invalid handler invoked"),
				_ => Assert.Fail("Invalid handler invoked"));
			structUnion3.Match(
				() => Assert.Fail("Invalid handler invoked"),
				(_, _) => Assert.Fail("Invalid handler invoked"),
				v1 => v1.Should().Be("value"),
				_ => Assert.Fail("Invalid handler invoked"));
			structUnion4.Match(
				() => Assert.Fail("Invalid handler invoked"),
				(_, _) => Assert.Fail("Invalid handler invoked"),
				_ => Assert.Fail("Invalid handler invoked"),
				v1 => v1.Should().Be(10));
		}

		[Fact]
		public void Match_ForMatchWithoutStateWithReturn_InvokeCorrectHandler()
		{
			// Arrange

			var union1 = TestNestedUnion.TestUnion<long>.Case1();
			var union2 = TestNestedUnion.TestUnion<long>.Case2("value", 2);
			var union3 = TestNestedUnion.TestUnion<long>.Case3("value");
			var union4 = TestNestedUnion.TestUnion<long>.Case4(10);

			var structUnion1 = TestStructUnion<long>.Case1();
			var structUnion2 = TestStructUnion<long>.Case2("value", 2);
			var structUnion3 = TestStructUnion<long>.Case3("value");
			var structUnion4 = TestStructUnion<long>.Case4(10);

			// Act and Assert

			union1.Match(() => "0", (v1, v2) => $"{v1} {v2}", v1 => v1, v1 => $"{v1}")
				.Should().Be("0", "Invalid handler invoked");
			union2.Match(() => "0", (v1, v2) => $"{v1} {v2}", v1 => v1, v1 => $"{v1}")
				.Should().Be("value 2", "Invalid handler invoked");
			union3.Match(() => "0", (v1, v2) => $"{v1} {v2}", v1 => v1, v1 => $"{v1}")
				.Should().Be("value", "Invalid handler invoked");
			union4.Match(() => "0", (v1, v2) => $"{v1} {v2}", v1 => v1, v1 => $"{v1}")
				.Should().Be("10", "Invalid handler invoked");

			structUnion1.Match(() => "0", (v1, v2) => $"{v1} {v2}", v1 => v1, v1 => $"{v1}")
				.Should().Be("0", "Invalid handler invoked");
			structUnion2.Match(() => "0", (v1, v2) => $"{v1} {v2}", v1 => v1, v1 => $"{v1}")
				.Should().Be("value 2", "Invalid handler invoked");
			structUnion3.Match(() => "0", (v1, v2) => $"{v1} {v2}", v1 => v1, v1 => $"{v1}")
				.Should().Be("value", "Invalid handler invoked");
			structUnion4.Match(() => "0", (v1, v2) => $"{v1} {v2}", v1 => v1, v1 => $"{v1}")
				.Should().Be("10", "Invalid handler invoked");
		}

		[Fact]
		public void Match_ForMatchWithStateWithoutReturn_InvokeCorrectHandler()
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

			union1.Match(
				"0",
				st => st.Should().Be("0"),
				(_, _, _) => Assert.Fail("Invalid handler invoked"),
				(_, _) => Assert.Fail("Invalid handler invoked"),
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
				(_, _) => Assert.Fail("Invalid handler invoked"),
				(_, _) => Assert.Fail("Invalid handler invoked"));
			union3.Match(
				"0",
				_ => Assert.Fail("Invalid handler invoked"),
				(_, _, _) => Assert.Fail("Invalid handler invoked"),
				(st, v1) =>
				{
					st.Should().Be("0");
					v1.Should().Be("value");
				},
				(_, _) => Assert.Fail("Invalid handler invoked"));
			union4.Match(
				"0",
				_ => Assert.Fail("Invalid handler invoked"),
				(_, _, _) => Assert.Fail("Invalid handler invoked"),
				(_, _) => Assert.Fail("Invalid handler invoked"),
				(st, v1) =>
				{
					st.Should().Be("0");
					v1.Should().Be(10);
				});

			structUnion1.Match(
				"0",
				st => st.Should().Be("0"),
				(_, _, _) => Assert.Fail("Invalid handler invoked"),
				(_, _) => Assert.Fail("Invalid handler invoked"),
				(_, _) => Assert.Fail("Invalid handler invoked"));
			structUnion2.Match(
				"0",
				_ => Assert.Fail("Invalid handler invoked"),
				(st, v1, v2) =>
				{
					st.Should().Be("0");
					v1.Should().Be("value");
					v2.Should().Be(2);
				},
				(_, _) => Assert.Fail("Invalid handler invoked"),
				(_, _) => Assert.Fail("Invalid handler invoked"));
			structUnion3.Match(
				"0",
				_ => Assert.Fail("Invalid handler invoked"),
				(_, _, _) => Assert.Fail("Invalid handler invoked"),
				(st, v1) =>
				{
					st.Should().Be("0");
					v1.Should().Be("value");
				},
				(_, _) => Assert.Fail("Invalid handler invoked"));
			structUnion4.Match(
				"0",
				_ => Assert.Fail("Invalid handler invoked"),
				(_, _, _) => Assert.Fail("Invalid handler invoked"),
				(_, _) => Assert.Fail("Invalid handler invoked"),
				(st, v1) =>
				{
					st.Should().Be("0");
					v1.Should().Be(10);
				});
		}

		[Fact]
		public void Match_ForMatchWithStateWithReturn_InvokeCorrectHandler()
		{
			// Arrange

			var union1 = TestNestedUnion.TestUnion<long>.Case1();
			var union2 = TestNestedUnion.TestUnion<long>.Case2("value", 2);
			var union3 = TestNestedUnion.TestUnion<long>.Case3("value");
			var union4 = TestNestedUnion.TestUnion<long>.Case4(10);

			var structUnion1 = TestStructUnion<long>.Case1();
			var structUnion2 = TestStructUnion<long>.Case2("value", 2);
			var structUnion3 = TestStructUnion<long>.Case3("value");
			var structUnion4 = TestStructUnion<long>.Case4(10);

			// Act and Assert

			union1.Match("0", st => st, (st, v1, v2) => $"{st} {v1} {v2}", (st, v1) => $"{st} {v1}", (st, v1) => $"{st} {v1}")
				.Should().Be("0", "Invalid handler invoked");
			union2.Match("0", st => st, (st, v1, v2) => $"{st} {v1} {v2}", (st, v1) => $"{st} {v1}", (st, v1) => $"{st} {v1}")
				.Should().Be("0 value 2", "Invalid handler invoked");
			union3.Match("0", st => st, (st, v1, v2) => $"{st} {v1} {v2}", (st, v1) => $"{st} {v1}", (st, v1) => $"{st} {v1}")
				.Should().Be("0 value", "Invalid handler invoked");
			union4.Match("0", st => st, (st, v1, v2) => $"{st} {v1} {v2}", (st, v1) => $"{st} {v1}", (st, v1) => $"{st} {v1}")
				.Should().Be("0 10", "Invalid handler invoked");

			structUnion1.Match("0", st => st, (st, v1, v2) => $"{st} {v1} {v2}", (st, v1) => $"{st} {v1}", (st, v1) => $"{st} {v1}")
				.Should().Be("0", "Invalid handler invoked");
			structUnion2.Match("0", st => st, (st, v1, v2) => $"{st} {v1} {v2}", (st, v1) => $"{st} {v1}", (st, v1) => $"{st} {v1}")
				.Should().Be("0 value 2", "Invalid handler invoked");
			structUnion3.Match("0", st => st, (st, v1, v2) => $"{st} {v1} {v2}", (st, v1) => $"{st} {v1}", (st, v1) => $"{st} {v1}")
				.Should().Be("0 value", "Invalid handler invoked");
			structUnion4.Match("0", st => st, (st, v1, v2) => $"{st} {v1} {v2}", (st, v1) => $"{st} {v1}", (st, v1) => $"{st} {v1}")
				.Should().Be("0 10", "Invalid handler invoked");
		}
	}
}