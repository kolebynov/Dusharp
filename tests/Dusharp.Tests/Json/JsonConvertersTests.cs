using System;
using System.Text.Json;
using Dusharp.Json;
using Dusharp.Tests.Json.TestHelpers;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace Dusharp.Tests.Json
{
	public class JsonConvertersTests
	{
		private static readonly JsonSerializerOptions DefaultConverterSerializerOptions = new()
		{
			Converters = { new DefaultUnionJsonConverter() },
		};

#if NET8_0_OR_GREATER
		private static readonly JsonSerializerOptions DefaultSpecializedConverterSerializerOptions = new()
		{
			Converters = { new DefaultUnionJsonConverter<TestUnion<int>>(), new DefaultUnionJsonConverter<TestStructUnion<int>>() },
		};
#endif

		private static readonly JsonSerializerOptions SpecificConverterSerializerOptions = new()
		{
			Converters = { new TestUnion<int>.JsonConverter(), new TestStructUnion<int>.JsonConverter() },
		};

		private static readonly JsonSerializerSettings NewtonsoftDefaultConverterSerializerSettings = new()
		{
			Converters = { new Newtonsoft.DefaultUnionJsonConverter() },
		};

		public static TheoryData<IJsonSerializer> UnionSerializers => new()
		{
			new StjJsonSerializer(DefaultConverterSerializerOptions),
#if NET8_0_OR_GREATER
			new StjJsonSerializer(DefaultSpecializedConverterSerializerOptions),
#endif
			new StjJsonSerializer(SpecificConverterSerializerOptions),
			new NewtonsoftJsonSerializer(NewtonsoftDefaultConverterSerializerSettings),
		};

		[Theory]
		[MemberData(nameof(UnionSerializers))]
		public void Write_ForDefaultStructValue_ThrowException(IJsonSerializer serializer)
		{
			// Arrange

			var errorMessage =
				$"Failed to serialize union {typeof(TestStructUnion<int>).Name}. It's in invalid state (probably a struct default value). (Parameter 'value')";

			// Act and Assert

			FluentActions.Invoking(() => serializer.Serialize<TestStructUnion<int>>(default)).Should().Throw<ArgumentException>()
				.WithMessage(errorMessage);
		}

		[Theory]
		[MemberData(nameof(UnionSerializers))]
		public void Write_ForParameterlessUnion_WriteOnlyCaseName(IJsonSerializer serializer)
		{
			// Act

			var classUnionJson = serializer.Serialize(TestUnion<int>.Case1());
			var structUnionJson = serializer.Serialize(TestStructUnion<int>.Case1());

			// Assert

			classUnionJson.Should().Be("\"Case1\"");
			structUnionJson.Should().Be("\"Case1\"");
		}

		[Theory]
		[MemberData(nameof(UnionSerializers))]
		public void Write_ForUnionWithOneParameter_WriteObjectWithCaseNameAndObjectWithParameterValue(IJsonSerializer serializer)
		{
			// Act

			var classUnionJson = serializer.Serialize(TestUnion<int>.Case3("test"));
			var structUnionJson = serializer.Serialize(TestStructUnion<int>.Case3("test"));

			// Assert

			classUnionJson.Should().Be("{\"Case3\":{\"value\":\"test\"}}");
			structUnionJson.Should().Be("{\"Case3\":{\"value\":\"test\"}}");
		}

		[Theory]
		[MemberData(nameof(UnionSerializers))]
		public void Write_ForUnionWithMultipleParameters_WriteObjectWithCaseNameAndObjectWithParameterValues(IJsonSerializer serializer)
		{
			// Act

			var classUnionJson = serializer.Serialize(TestUnion<int>.Case2("test", 2));
			var structUnionJson = serializer.Serialize(TestStructUnion<int>.Case2("test", 2));

			// Assert

			classUnionJson.Should().Be("{\"Case2\":{\"value1\":\"test\",\"value2\":2}}");
			structUnionJson.Should().Be("{\"Case2\":{\"value1\":\"test\",\"value2\":2}}");
		}

		[Theory]
		[MemberData(nameof(UnionSerializers))]
		public void Read_ForParameterlessUnion_ReadCorrectly(IJsonSerializer serializer)
		{
			// Act<TestUnion<int>>

			var classUnion = serializer.Deserialize<TestUnion<int>>("\"Case1\"");
			var structUnion = serializer.Deserialize<TestStructUnion<int>>("\"Case1\"");

			// Assert

			classUnion.Should().Be(TestUnion<int>.Case1());
			structUnion.Should().Be(TestStructUnion<int>.Case1());
		}

		[Theory]
		[MemberData(nameof(UnionSerializers))]
		public void Read_ForUnionWithOneParameter_ReadCorrectly(IJsonSerializer serializer)
		{
			// Act

			var classUnion = serializer.Deserialize<TestUnion<int>>("{\"Case3\":{\"value\":\"test\"}}");
			var structUnion = serializer.Deserialize<TestStructUnion<int>>("{\"Case3\":{\"value\":\"test\"}}");

			// Assert

			classUnion.Should().Be(TestUnion<int>.Case3("test"));
			structUnion.Should().Be(TestStructUnion<int>.Case3("test"));
		}

		[Theory]
		[MemberData(nameof(UnionSerializers))]
		public void Read_ForUnionWithMultipleParameters_ReadCorrectly(IJsonSerializer serializer)
		{
			// Act

			var classUnion = serializer.Deserialize<TestUnion<int>>("{\"Case2\":{\"value1\":\"test\",\"value2\":2}}");
			var structUnion = serializer.Deserialize<TestStructUnion<int>>("{\"Case2\":{\"value1\":\"test\",\"value2\":2}}");

			// Assert

			classUnion.Should().Be(TestUnion<int>.Case2("test", 2));
			structUnion.Should().Be(TestStructUnion<int>.Case2("test", 2));
		}

		[Theory]
		[MemberData(nameof(UnionSerializers))]
		public void Read_ForInvalidStartToken_ThrowException(IJsonSerializer serializer)
		{
			// Act and Assert

			FluentActions.Invoking(() => serializer.Deserialize<TestUnion<int>>("2"))
				.Should()
				.Throw<Exception>()
				.WithMessage("Invalid start token *. Expected \"StartObject\" or \"String\".");

			FluentActions.Invoking(() => serializer.Deserialize<TestStructUnion<int>>("true"))
				.Should()
				.Throw<Exception>()
				.WithMessage("Invalid start token *. Expected \"StartObject\" or \"String\".");
		}

		[Theory]
		[MemberData(nameof(UnionSerializers))]
		public void Read_ForEmptyObject_ThrowException(IJsonSerializer serializer)
		{
			// Arrange

			var expectedErrorMessage =
				"There is an invalid union JSON object. It must contain property with case name. There is a token \"EndObject\".";

			// Act and Assert

			FluentActions.Invoking(() => serializer.Deserialize<TestUnion<int>>("{}"))
				.Should().Throw<Exception>().WithMessage(expectedErrorMessage);
			FluentActions.Invoking(() => serializer.Deserialize<TestStructUnion<int>>("{}"))
				.Should().Throw<Exception>().WithMessage(expectedErrorMessage);
		}

		[Theory]
		[MemberData(nameof(UnionSerializers))]
		public void Read_ForInvalidParameterlessCase_ThrowException(IJsonSerializer serializer)
		{
			// Act and Assert

			FluentActions.Invoking(() => serializer.Deserialize<TestUnion<int>>("\"InvalidCase\""))
				.Should()
				.Throw<Exception>()
				.WithMessage(
					$"There is no parameterless case named \"InvalidCase\" in union \"{typeof(TestUnion<int>).Name}\".");

			FluentActions.Invoking(() => serializer.Deserialize<TestStructUnion<int>>("\"InvalidCase\""))
				.Should()
				.Throw<Exception>()
				.WithMessage(
					$"There is no parameterless case named \"InvalidCase\" in union \"{typeof(TestStructUnion<int>).Name}\".");
		}

		[Theory]
		[MemberData(nameof(UnionSerializers))]
		public void Read_ForInvalidWithParametersCase_ThrowException(IJsonSerializer serializer)
		{
			// Act and Assert

			FluentActions.Invoking(() => serializer.Deserialize<TestUnion<int>>("{\"InvalidCase\":{}}"))
				.Should()
				.Throw<Exception>()
				.WithMessage(
					$"There is no case named \"InvalidCase\" in union \"{typeof(TestUnion<int>).Name}\".");

			FluentActions.Invoking(() => serializer.Deserialize<TestStructUnion<int>>("{\"InvalidCase\":{}}"))
				.Should()
				.Throw<Exception>()
				.WithMessage(
					$"There is no case named \"InvalidCase\" in union \"{typeof(TestStructUnion<int>).Name}\".");
		}

		[Theory]
		[MemberData(nameof(UnionSerializers))]
		public void Read_ForIncompleteCase_ThrowException(IJsonSerializer serializer)
		{
			// Act and Assert

			FluentActions.Invoking(() => serializer.Deserialize<TestUnion<int>>("{\"Case2\":{\"value2\":2}}"))
				.Should()
				.Throw<Exception>()
				.WithMessage(
					$"Not all parameters are present in json for union case \"Case2\" of union \"{typeof(TestUnion<int>).Name}\". Expected: 2, present: 1.");

			FluentActions.Invoking(() => serializer.Deserialize<TestStructUnion<int>>("{\"Case2\":{\"value2\":2}}"))
				.Should()
				.Throw<Exception>()
				.WithMessage(
					$"Not all parameters are present in json for union case \"Case2\" of union \"{typeof(TestStructUnion<int>).Name}\". Expected: 2, present: 1.");
		}

		[Theory]
		[MemberData(nameof(UnionSerializers))]
		public void Read_ForUnionInsideObjects_ReadCorrectly(IJsonSerializer serializer)
		{
			// Act

			var classUnion1 = serializer.Deserialize<ClassWithUnion<TestUnion<int>>>(
				"{\"Union\":\"Case1\",\"SomeString\":\"test\"}");
			var structUnion1 = serializer.Deserialize<ClassWithUnion<TestStructUnion<int>>>(
				"{\"Union\":\"Case1\",\"SomeString\":\"test\"}");
			var classUnion2 = serializer.Deserialize<ClassWithUnion<TestUnion<int>>>(
				"{\"Union\":{\"Case2\":{\"value1\":\"test\",\"value2\":2}},\"SomeString\":\"test\"}");
			var structUnion2 = serializer.Deserialize<ClassWithUnion<TestStructUnion<int>>>(
				"{\"Union\":{\"Case2\":{\"value1\":\"test\",\"value2\":2}},\"SomeString\":\"test\"}");

			// Assert

			classUnion1.Should().BeEquivalentTo(new ClassWithUnion<TestUnion<int>>
			{
				Union = TestUnion<int>.Case1(),
				SomeString = "test",
			});
			structUnion1.Should().BeEquivalentTo(new ClassWithUnion<TestStructUnion<int>>
			{
				Union = TestStructUnion<int>.Case1(),
				SomeString = "test",
			});
			classUnion2.Should().BeEquivalentTo(new ClassWithUnion<TestUnion<int>>
			{
				Union = TestUnion<int>.Case2("test", 2),
				SomeString = "test",
			});
			structUnion2.Should().BeEquivalentTo(new ClassWithUnion<TestStructUnion<int>>
			{
				Union = TestStructUnion<int>.Case2("test", 2),
				SomeString = "test",
			});
		}

		[Theory]
		[MemberData(nameof(UnionSerializers))]
		public void Read_ForUnionArray_ReadCorrectly(IJsonSerializer serializer)
		{
			// Act

			var classUnions = serializer.Deserialize<UnionArray<TestUnion<int>>>(
				"{\"Unions\":[\"Case1\",{\"Case2\": {\"value1\":\"test\",\"value2\":2}},{\"Case4\":{\"value\":10}}]}");
			var structUnions = serializer.Deserialize<UnionArray<TestStructUnion<int>>>(
				"{\"Unions\":[\"Case1\",{\"Case2\": {\"value1\":\"test\",\"value2\":2}},{\"Case4\":{\"value\":10}}]}");

			// Assert

			classUnions.Should().BeEquivalentTo(new UnionArray<TestUnion<int>>
			{
				Unions = new[]
				{
					TestUnion<int>.Case1(), TestUnion<int>.Case2("test", 2), TestUnion<int>.Case4(10),
				},
			});
			structUnions.Should().BeEquivalentTo(new UnionArray<TestStructUnion<int>>
			{
				Unions = new[]
				{
					TestStructUnion<int>.Case1(), TestStructUnion<int>.Case2("test", 2), TestStructUnion<int>.Case4(10),
				},
			});
		}

		private sealed class ClassWithUnion<TUnion>
		{
			public TUnion Union { get; init; } = default!;

			public string SomeString { get; init; } = null!;
		}

		private sealed class UnionArray<TUnion>
		{
			public TUnion[] Unions { get; init; } = null!;
		}
	}
}