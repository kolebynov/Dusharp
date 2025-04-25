using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Dusharp.Json;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;
using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Dusharp.Tests.Json
{
	public class JsonConvertersTests
	{
		private static readonly JsonSerializerOptions DefaultConverterSerializerOptions = new()
		{
			Converters = { new DefaultUnionJsonConverter() },
		};

		private static readonly JsonSerializerOptions ClassConverterSerializerOptions = new()
		{
			Converters = { new TestUnion<int>.JsonConverter() },
		};

		private static readonly JsonSerializerOptions StructConverterSerializerOptions = new()
		{
			Converters = { new TestStructUnion<int>.JsonConverter() },
		};

		private static readonly JsonSerializerSettings NewtonsoftDefaultConverterSerializerSettings = new()
		{
			Converters = { new Newtonsoft.DefaultUnionJsonConverter() },
		};

		public static IEnumerable<object[]> Write_ForDefaultStructValue_Data => new[]
		{
			new Func<TestStructUnion<int>, string>[] { union => JsonSerializer.Serialize(union, DefaultConverterSerializerOptions) },
			new Func<TestStructUnion<int>, string>[] { union => JsonSerializer.Serialize(union, StructConverterSerializerOptions) },
			new Func<TestStructUnion<int>, string>[] { union => JsonConvert.SerializeObject(union, NewtonsoftDefaultConverterSerializerSettings) },
		};

		[Theory]
		[MemberData(nameof(Write_ForDefaultStructValue_Data))]
		public void Write_ForDefaultStructValue_ThrowException(Func<TestStructUnion<int>, string> serializeFunc)
		{
			// Arrange

			var errorMessage =
				$"Failed to serialize union {typeof(TestStructUnion<int>).Name}. It's in invalid state (probably a struct default value). (Parameter 'value')";

			// Act and Assert

			FluentActions.Invoking(() => serializeFunc(default)).Should().Throw<ArgumentException>()
				.WithMessage(errorMessage);
		}

		public static IEnumerable<object[]> UnionSerializers => new[]
		{
			new object[]
			{
				new Func<TestUnion<int>, string>(u => JsonSerializer.Serialize(u, DefaultConverterSerializerOptions)),
				new Func<TestStructUnion<int>, string>(u =>
					JsonSerializer.Serialize(u, DefaultConverterSerializerOptions)),
			},
			new object[]
			{
				new Func<TestUnion<int>, string>(u => JsonSerializer.Serialize(u, ClassConverterSerializerOptions)),
				new Func<TestStructUnion<int>, string>(u =>
					JsonSerializer.Serialize(u, StructConverterSerializerOptions)),
			},
			new object[]
			{
				new Func<TestUnion<int>, string>(u =>
					JsonConvert.SerializeObject(u, NewtonsoftDefaultConverterSerializerSettings)),
				new Func<TestStructUnion<int>, string>(u =>
					JsonConvert.SerializeObject(u, NewtonsoftDefaultConverterSerializerSettings)),
			},
		};

		[Theory]
		[MemberData(nameof(UnionSerializers))]
		public void Write_ForParameterlessUnion_WriteOnlyCaseName(
			Func<TestUnion<int>, string> classUnionSerializeFunc,
			Func<TestStructUnion<int>, string> structUnionSerializeFunc)
		{
			// Act

			var classUnionJson = classUnionSerializeFunc(TestUnion<int>.Case1());
			var structUnionJson = structUnionSerializeFunc(TestStructUnion<int>.Case1());

			// Assert

			classUnionJson.Should().Be("\"Case1\"");
			structUnionJson.Should().Be("\"Case1\"");
		}

		[Theory]
		[MemberData(nameof(UnionSerializers))]
		public void Write_ForUnionWithOneParameter_WriteObjectWithCaseNameAndObjectWithParameterValue(
			Func<TestUnion<int>, string> classUnionSerializeFunc,
			Func<TestStructUnion<int>, string> structUnionSerializeFunc)
		{
			// Act

			var classUnionJson = classUnionSerializeFunc(TestUnion<int>.Case3("test"));
			var structUnionJson = structUnionSerializeFunc(TestStructUnion<int>.Case3("test"));

			// Assert

			classUnionJson.Should().Be("{\"Case3\":{\"value\":\"test\"}}");
			structUnionJson.Should().Be("{\"Case3\":{\"value\":\"test\"}}");
		}

		[Theory]
		[MemberData(nameof(UnionSerializers))]
		public void Write_ForUnionWithMultipleParameters_WriteObjectWithCaseNameAndObjectWithParameterValues(
			Func<TestUnion<int>, string> classUnionSerializeFunc,
			Func<TestStructUnion<int>, string> structUnionSerializeFunc)
		{
			// Act

			var classUnionJson = classUnionSerializeFunc(TestUnion<int>.Case2("test", 2));
			var structUnionJson = structUnionSerializeFunc(TestStructUnion<int>.Case2("test", 2));

			// Assert

			classUnionJson.Should().Be("{\"Case2\":{\"value1\":\"test\",\"value2\":2}}");
			structUnionJson.Should().Be("{\"Case2\":{\"value1\":\"test\",\"value2\":2}}");
		}

		public static IEnumerable<object[]> UnionDeserializers => new[]
		{
			new object[]
			{
				new Func<string, TestUnion<int>>(str => JsonSerializer.Deserialize<TestUnion<int>>(str, DefaultConverterSerializerOptions)!),
				new Func<string, TestStructUnion<int>>(str => JsonSerializer.Deserialize<TestStructUnion<int>>(str, DefaultConverterSerializerOptions)),
			},
			new object[]
			{
				new Func<string, TestUnion<int>>(str => JsonSerializer.Deserialize<TestUnion<int>>(str, ClassConverterSerializerOptions)!),
				new Func<string, TestStructUnion<int>>(str => JsonSerializer.Deserialize<TestStructUnion<int>>(str, StructConverterSerializerOptions)),
			},
			new object[]
			{
				new Func<string, TestUnion<int>>(str => JsonConvert.DeserializeObject<TestUnion<int>>(str, NewtonsoftDefaultConverterSerializerSettings)!),
				new Func<string, TestStructUnion<int>>(str => JsonConvert.DeserializeObject<TestStructUnion<int>>(str, NewtonsoftDefaultConverterSerializerSettings)),
			},
		};

		[Theory]
		[MemberData(nameof(UnionDeserializers))]
		public void Read_ForParameterlessUnion_ReadCorrectly(
			Func<string, TestUnion<int>> classUnionDeserializeFunc,
			Func<string, TestStructUnion<int>> structUnionDeserializeFunc)
		{
			// Act

			var classUnion = classUnionDeserializeFunc("\"Case1\"");
			var structUnion = structUnionDeserializeFunc("\"Case1\"");

			// Assert

			classUnion.Should().Be(TestUnion<int>.Case1());
			structUnion.Should().Be(TestStructUnion<int>.Case1());
		}

		[Theory]
		[MemberData(nameof(UnionDeserializers))]
		public void Read_ForUnionWithOneParameter_ReadCorrectly(
			Func<string, TestUnion<int>> classUnionDeserializeFunc,
			Func<string, TestStructUnion<int>> structUnionDeserializeFunc)
		{
			// Act

			var classUnion = classUnionDeserializeFunc("{\"Case3\":{\"value\":\"test\"}}");
			var structUnion = structUnionDeserializeFunc("{\"Case3\":{\"value\":\"test\"}}");

			// Assert

			classUnion.Should().Be(TestUnion<int>.Case3("test"));
			structUnion.Should().Be(TestStructUnion<int>.Case3("test"));
		}

		[Theory]
		[MemberData(nameof(UnionDeserializers))]
		public void Read_ForUnionWithMultipleParameters_ReadCorrectly(
			Func<string, TestUnion<int>> classUnionDeserializeFunc,
			Func<string, TestStructUnion<int>> structUnionDeserializeFunc)
		{
			// Act

			var classUnion = classUnionDeserializeFunc("{\"Case2\":{\"value1\":\"test\",\"value2\":2}}");
			var structUnion = structUnionDeserializeFunc("{\"Case2\":{\"value1\":\"test\",\"value2\":2}}");

			// Assert

			classUnion.Should().Be(TestUnion<int>.Case2("test", 2));
			structUnion.Should().Be(TestStructUnion<int>.Case2("test", 2));
		}

		[Theory]
		[MemberData(nameof(UnionDeserializers))]
		public void Read_ForInvalidStartToken_ThrowException(
			Func<string, TestUnion<int>> classUnionDeserializeFunc,
			Func<string, TestStructUnion<int>> structUnionDeserializeFunc)
		{
			// Act and Assert

			FluentActions.Invoking(() => classUnionDeserializeFunc("2"))
				.Should()
				.Throw<Exception>()
				.WithMessage("Invalid start token *. Expected \"StartObject\" or \"String\".");

			FluentActions.Invoking(() => structUnionDeserializeFunc("true"))
				.Should()
				.Throw<Exception>()
				.WithMessage("Invalid start token *. Expected \"StartObject\" or \"String\".");
		}

		[Theory]
		[MemberData(nameof(UnionDeserializers))]
		public void Read_ForEmptyObject_ThrowException(
			Func<string, TestUnion<int>> classUnionDeserializeFunc,
			Func<string, TestStructUnion<int>> structUnionDeserializeFunc)
		{
			// Arrange

			var expectedErrorMessage =
				"There is an invalid union JSON object. It must contain property with case name. There is a token \"EndObject\".";

			// Act and Assert

			FluentActions.Invoking(() => classUnionDeserializeFunc("{}"))
				.Should().Throw<Exception>().WithMessage(expectedErrorMessage);
			FluentActions.Invoking(() => structUnionDeserializeFunc("{}"))
				.Should().Throw<Exception>().WithMessage(expectedErrorMessage);
		}

		[Theory]
		[MemberData(nameof(UnionDeserializers))]
		public void Read_ForInvalidParameterlessCase_ThrowException(
			Func<string, TestUnion<int>> classUnionDeserializeFunc,
			Func<string, TestStructUnion<int>> structUnionDeserializeFunc)
		{
			// Act and Assert

			FluentActions.Invoking(() => classUnionDeserializeFunc("\"InvalidCase\""))
				.Should()
				.Throw<Exception>()
				.WithMessage(
					$"There is no parameterless case named \"InvalidCase\" in union \"{typeof(TestUnion<int>).Name}\".");

			FluentActions.Invoking(() => structUnionDeserializeFunc("\"InvalidCase\""))
				.Should()
				.Throw<Exception>()
				.WithMessage(
					$"There is no parameterless case named \"InvalidCase\" in union \"{typeof(TestStructUnion<int>).Name}\".");
		}

		[Theory]
		[MemberData(nameof(UnionDeserializers))]
		public void Read_ForInvalidWithParametersCase_ThrowException(
			Func<string, TestUnion<int>> classUnionDeserializeFunc,
			Func<string, TestStructUnion<int>> structUnionDeserializeFunc)
		{
			// Act and Assert

			FluentActions.Invoking(() => classUnionDeserializeFunc("{\"InvalidCase\":{}}"))
				.Should()
				.Throw<Exception>()
				.WithMessage(
					$"There is no case named \"InvalidCase\" in union \"{typeof(TestUnion<int>).Name}\".");

			FluentActions.Invoking(() => structUnionDeserializeFunc("{\"InvalidCase\":{}}"))
				.Should()
				.Throw<Exception>()
				.WithMessage(
					$"There is no case named \"InvalidCase\" in union \"{typeof(TestStructUnion<int>).Name}\".");
		}

		[Theory]
		[MemberData(nameof(UnionDeserializers))]
		public void Read_ForIncompleteCase_ThrowException(
			Func<string, TestUnion<int>> classUnionDeserializeFunc,
			Func<string, TestStructUnion<int>> structUnionDeserializeFunc)
		{
			// Act and Assert

			FluentActions.Invoking(() => classUnionDeserializeFunc("{\"Case2\":{\"value2\":2}}"))
				.Should()
				.Throw<Exception>()
				.WithMessage(
					$"Not all parameters are present in json for union case \"Case2\" of union \"{typeof(TestUnion<int>).Name}\". Expected: 2, present: 1.");

			FluentActions.Invoking(() => structUnionDeserializeFunc("{\"Case2\":{\"value2\":2}}"))
				.Should()
				.Throw<Exception>()
				.WithMessage(
					$"Not all parameters are present in json for union case \"Case2\" of union \"{typeof(TestStructUnion<int>).Name}\". Expected: 2, present: 1.");
		}

		[Theory]
		[MemberData(nameof(UnionDeserializers))]
		public void Read_IfUnionJsonContainsMoreData_ThrowException(
			Func<string, TestUnion<int>> classUnionDeserializeFunc,
			Func<string, TestStructUnion<int>> structUnionDeserializeFunc)
		{
			// Act and Assert

			FluentActions.Invoking(() =>
					classUnionDeserializeFunc("{\"Case3\":{\"value\":\"test\"},\"invalid\":\"yes\"}"))
				.Should()
				.Throw<Exception>()
				.WithMessage(
					$"Unexpected end of union JSON. Token: \"{JsonTokenType.PropertyName}\", union: \"{typeof(TestUnion<int>).Name}\".");

			FluentActions.Invoking(() =>
					structUnionDeserializeFunc("{\"Case3\":{\"value\":\"test\"},\"invalid\":\"yes\"}"))
				.Should()
				.Throw<Exception>()
				.WithMessage(
					$"Unexpected end of union JSON. Token: \"{JsonTokenType.PropertyName}\", union: \"{typeof(TestStructUnion<int>).Name}\".");
		}
	}
}