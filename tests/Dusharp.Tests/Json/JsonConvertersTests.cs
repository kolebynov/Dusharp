using System.Text.Json;
using Dusharp.Json;
using FluentAssertions;
using Xunit;

namespace Dusharp.Tests.Json
{
	public class JsonConvertersTests
	{
		private readonly JsonSerializerOptions _defaultConverterSerializerOptions = new()
		{
			Converters = { new DefaultUnionJsonConverter() },
		};

		private readonly JsonSerializerOptions _classConverterSerializerOptions = new()
		{
			Converters = { new TestUnion<int>.JsonConverter() },
		};

		private readonly JsonSerializerOptions _structConverterSerializerOptions = new()
		{
			Converters = { new TestStructUnion<int>.JsonConverter() },
		};

		[Fact]
		public void Write_ForDefaultValue_WriteEmptyObject()
		{
			// Act

			var resultJson = JsonSerializer.Serialize(default(TestStructUnion<int>), _defaultConverterSerializerOptions);

			// Assert

			resultJson.Should().Be("{}");
		}

		[Fact]
		public void Write_ForParameterlessUnion_WriteOnlyCaseName()
		{
			// Act

			var resultJson1 = JsonSerializer.Serialize(TestUnion<int>.Case1(), _defaultConverterSerializerOptions);
			var resultJson2 = JsonSerializer.Serialize(TestStructUnion<int>.Case1(), _defaultConverterSerializerOptions);
			var resultJson3 = JsonSerializer.Serialize(TestUnion<int>.Case1(), _classConverterSerializerOptions);
			var resultJson4 = JsonSerializer.Serialize(TestStructUnion<int>.Case1(), _structConverterSerializerOptions);

			// Assert

			resultJson1.Should().Be("\"Case1\"");
			resultJson2.Should().Be("\"Case1\"");
			resultJson3.Should().Be("\"Case1\"");
			resultJson4.Should().Be("\"Case1\"");
		}

		[Fact]
		public void Write_ForUnionWithOneParameter_WriteObjectWithCaseNameAndObjectWithParameterValue()
		{
			// Act

			var resultJson1 = JsonSerializer.Serialize(TestUnion<int>.Case3("test"), _defaultConverterSerializerOptions);
			var resultJson2 = JsonSerializer.Serialize(TestStructUnion<int>.Case3("test"), _defaultConverterSerializerOptions);
			var resultJson3 = JsonSerializer.Serialize(TestUnion<int>.Case3("test"), _classConverterSerializerOptions);
			var resultJson4 = JsonSerializer.Serialize(TestStructUnion<int>.Case3("test"), _structConverterSerializerOptions);

			// Assert

			resultJson1.Should().Be("{\"Case3\":{\"value\":\"test\"}}");
			resultJson2.Should().Be("{\"Case3\":{\"value\":\"test\"}}");
			resultJson3.Should().Be("{\"Case3\":{\"value\":\"test\"}}");
			resultJson4.Should().Be("{\"Case3\":{\"value\":\"test\"}}");
		}

		[Fact]
		public void Write_ForUnionWithMultipleParameters_WriteObjectWithCaseNameAndObjectWithParameterValues()
		{
			// Act

			var resultJson1 = JsonSerializer.Serialize(TestUnion<int>.Case2("test", 2), _defaultConverterSerializerOptions);
			var resultJson2 = JsonSerializer.Serialize(TestStructUnion<int>.Case2("test", 2), _defaultConverterSerializerOptions);
			var resultJson3 = JsonSerializer.Serialize(TestUnion<int>.Case2("test", 2), _classConverterSerializerOptions);
			var resultJson4 = JsonSerializer.Serialize(TestStructUnion<int>.Case2("test", 2), _structConverterSerializerOptions);

			// Assert

			resultJson1.Should().Be("{\"Case2\":{\"value1\":\"test\",\"value2\":2}}");
			resultJson2.Should().Be("{\"Case2\":{\"value1\":\"test\",\"value2\":2}}");
			resultJson3.Should().Be("{\"Case2\":{\"value1\":\"test\",\"value2\":2}}");
			resultJson4.Should().Be("{\"Case2\":{\"value1\":\"test\",\"value2\":2}}");
		}

		[Fact]
		public void Read_ForParameterlessUnion_ReadCorrectly()
		{
			// Act

			var result1 = JsonSerializer.Deserialize<TestUnion<int>>("\"Case1\"", _defaultConverterSerializerOptions);
			var result2 = JsonSerializer.Deserialize<TestStructUnion<int>>("\"Case1\"", _defaultConverterSerializerOptions);
			var result3 = JsonSerializer.Deserialize<TestUnion<int>>("\"Case1\"", _classConverterSerializerOptions);
			var result4 = JsonSerializer.Deserialize<TestStructUnion<int>>("\"Case1\"", _structConverterSerializerOptions);

			// Assert

			result1.Should().Be(TestUnion<int>.Case1());
			result2.Should().Be(TestStructUnion<int>.Case1());
			result3.Should().Be(TestUnion<int>.Case1());
			result4.Should().Be(TestStructUnion<int>.Case1());
		}

		[Fact]
		public void Read_ForUnionWithOneParameter_ReadCorrectly()
		{
			// Act

			var result1 = JsonSerializer.Deserialize<TestUnion<int>>("{\"Case3\":{\"value\":\"test\"}}", _defaultConverterSerializerOptions);
			var result2 = JsonSerializer.Deserialize<TestStructUnion<int>>("{\"Case3\":{\"value\":\"test\"}}", _defaultConverterSerializerOptions);
			var result3 = JsonSerializer.Deserialize<TestUnion<int>>("{\"Case3\":{\"value\":\"test\"}}", _classConverterSerializerOptions);
			var result4 = JsonSerializer.Deserialize<TestStructUnion<int>>("{\"Case3\":{\"value\":\"test\"}}", _structConverterSerializerOptions);

			// Assert

			result1.Should().Be(TestUnion<int>.Case3("test"));
			result2.Should().Be(TestStructUnion<int>.Case3("test"));
			result3.Should().Be(TestUnion<int>.Case3("test"));
			result4.Should().Be(TestStructUnion<int>.Case3("test"));
		}

		[Fact]
		public void Read_ForUnionWithMultipleParameters_ReadCorrectly()
		{
			// Act

			var result1 = JsonSerializer.Deserialize<TestUnion<int>>("{\"Case2\":{\"value1\":\"test\",\"value2\":2}}", _defaultConverterSerializerOptions);
			var result2 = JsonSerializer.Deserialize<TestStructUnion<int>>("{\"Case2\":{\"value1\":\"test\",\"value2\":2}}", _defaultConverterSerializerOptions);
			var result3 = JsonSerializer.Deserialize<TestUnion<int>>("{\"Case2\":{\"value1\":\"test\",\"value2\":2}}", _classConverterSerializerOptions);
			var result4 = JsonSerializer.Deserialize<TestStructUnion<int>>("{\"Case2\":{\"value1\":\"test\",\"value2\":2}}", _structConverterSerializerOptions);

			// Assert

			result1.Should().Be(TestUnion<int>.Case2("test", 2));
			result2.Should().Be(TestStructUnion<int>.Case2("test", 2));
			result3.Should().Be(TestUnion<int>.Case2("test", 2));
			result4.Should().Be(TestStructUnion<int>.Case2("test", 2));
		}

		[Fact]
		public void Read_ForInvalidStartToken_ThrowException()
		{
			// Act and Assert

			FluentActions.Invoking(() => JsonSerializer.Deserialize<TestUnion<int>>("2", _defaultConverterSerializerOptions))
				.Should()
				.Throw<JsonException>()
				.WithMessage(
					$"Invalid start token \"{JsonTokenType.Number}\" when deserializing \"{typeof(TestUnion<int>).Name}\" union. Expected \"StartObject\" or \"String\".");
			FluentActions.Invoking(() => JsonSerializer.Deserialize<TestStructUnion<int>>("true", _defaultConverterSerializerOptions))
				.Should()
				.Throw<JsonException>()
				.WithMessage(
					$"Invalid start token \"{JsonTokenType.True}\" when deserializing \"{typeof(TestStructUnion<int>).Name}\" union. Expected \"StartObject\" or \"String\".");
			FluentActions.Invoking(() => JsonSerializer.Deserialize<TestUnion<int>>("2", _classConverterSerializerOptions))
				.Should()
				.Throw<JsonException>()
				.WithMessage(
					$"Invalid start token \"{JsonTokenType.Number}\" when deserializing \"{typeof(TestUnion<int>).Name}\" union. Expected \"StartObject\" or \"String\".");
			FluentActions.Invoking(() => JsonSerializer.Deserialize<TestStructUnion<int>>("true", _structConverterSerializerOptions))
				.Should()
				.Throw<JsonException>()
				.WithMessage(
					$"Invalid start token \"{JsonTokenType.True}\" when deserializing \"{typeof(TestStructUnion<int>).Name}\" union. Expected \"StartObject\" or \"String\".");
		}

		[Fact]
		public void Read_ForEmptyObject_ThrowException()
		{
			// Arrange

			var expectedErrorMessage = "There is an invalid union JSON object. It must contain property with case name. There is a token \"EndObject\".";

			// Act and Assert

			FluentActions.Invoking(() => JsonSerializer.Deserialize<TestUnion<int>>("{}", _defaultConverterSerializerOptions))
				.Should().Throw<JsonException>().WithMessage(expectedErrorMessage);
			FluentActions.Invoking(() => JsonSerializer.Deserialize<TestStructUnion<int>>("{}", _defaultConverterSerializerOptions))
				.Should().Throw<JsonException>().WithMessage(expectedErrorMessage);
			FluentActions.Invoking(() => JsonSerializer.Deserialize<TestUnion<int>>("{}", _classConverterSerializerOptions))
				.Should().Throw<JsonException>().WithMessage(expectedErrorMessage);
			FluentActions.Invoking(() => JsonSerializer.Deserialize<TestStructUnion<int>>("{}", _structConverterSerializerOptions))
				.Should().Throw<JsonException>().WithMessage(expectedErrorMessage);
		}

		[Fact]
		public void Read_ForInvalidParameterlessCase_ThrowException()
		{
			// Act and Assert

			FluentActions.Invoking(() => JsonSerializer.Deserialize<TestUnion<int>>("\"InvalidCase\"", _defaultConverterSerializerOptions))
				.Should()
				.Throw<JsonException>()
				.WithMessage(
					$"There is no parameterless case named \"InvalidCase\" in union \"{typeof(TestUnion<int>).Name}\".");
			FluentActions.Invoking(() => JsonSerializer.Deserialize<TestStructUnion<int>>("\"InvalidCase\"", _defaultConverterSerializerOptions))
				.Should()
				.Throw<JsonException>()
				.WithMessage(
					$"There is no parameterless case named \"InvalidCase\" in union \"{typeof(TestStructUnion<int>).Name}\".");
			FluentActions.Invoking(() => JsonSerializer.Deserialize<TestUnion<int>>("\"InvalidCase\"", _classConverterSerializerOptions))
				.Should()
				.Throw<JsonException>()
				.WithMessage(
					$"There is no parameterless case named \"InvalidCase\" in union \"{typeof(TestUnion<int>).Name}\".");
			FluentActions.Invoking(() => JsonSerializer.Deserialize<TestStructUnion<int>>("\"InvalidCase\"", _structConverterSerializerOptions))
				.Should()
				.Throw<JsonException>()
				.WithMessage(
					$"There is no parameterless case named \"InvalidCase\" in union \"{typeof(TestStructUnion<int>).Name}\".");
		}

		[Fact]
		public void Read_ForInvalidWithParametersCase_ThrowException()
		{
			// Act and Assert

			FluentActions.Invoking(() => JsonSerializer.Deserialize<TestUnion<int>>("{\"InvalidCase\":{}}", _defaultConverterSerializerOptions))
				.Should()
				.Throw<JsonException>()
				.WithMessage(
					$"There is no case named \"InvalidCase\" in union \"{typeof(TestUnion<int>).Name}\".");
			FluentActions.Invoking(() => JsonSerializer.Deserialize<TestStructUnion<int>>("{\"InvalidCase\":{}}", _defaultConverterSerializerOptions))
				.Should()
				.Throw<JsonException>()
				.WithMessage(
					$"There is no case named \"InvalidCase\" in union \"{typeof(TestStructUnion<int>).Name}\".");
			FluentActions.Invoking(() => JsonSerializer.Deserialize<TestUnion<int>>("{\"InvalidCase\":{}}", _classConverterSerializerOptions))
				.Should()
				.Throw<JsonException>()
				.WithMessage(
					$"There is no case named \"InvalidCase\" in union \"{typeof(TestUnion<int>).Name}\".");
			FluentActions.Invoking(() => JsonSerializer.Deserialize<TestStructUnion<int>>("{\"InvalidCase\":{}}", _structConverterSerializerOptions))
				.Should()
				.Throw<JsonException>()
				.WithMessage(
					$"There is no case named \"InvalidCase\" in union \"{typeof(TestStructUnion<int>).Name}\".");
		}

		[Fact]
		public void Read_ForIncompleteCase_ThrowException()
		{
			// Act and Assert

			FluentActions.Invoking(() => JsonSerializer.Deserialize<TestUnion<int>>("{\"Case2\":{\"value2\":2}}", _defaultConverterSerializerOptions))
				.Should()
				.Throw<JsonException>()
				.WithMessage(
					$"Not all parameters are present in json for union case \"Case2\" of union \"{typeof(TestUnion<int>).Name}\". Expected: 2, present: 1.");
			FluentActions.Invoking(() => JsonSerializer.Deserialize<TestStructUnion<int>>("{\"Case2\":{\"value2\":2}}", _defaultConverterSerializerOptions))
				.Should()
				.Throw<JsonException>()
				.WithMessage(
					$"Not all parameters are present in json for union case \"Case2\" of union \"{typeof(TestStructUnion<int>).Name}\". Expected: 2, present: 1.");
			FluentActions.Invoking(() => JsonSerializer.Deserialize<TestUnion<int>>("{\"Case2\":{\"value2\":2}}", _classConverterSerializerOptions))
				.Should()
				.Throw<JsonException>()
				.WithMessage(
					$"Not all parameters are present in json for union case \"Case2\" of union \"{typeof(TestUnion<int>).Name}\". Expected: 2, present: 1.");
			FluentActions.Invoking(() => JsonSerializer.Deserialize<TestStructUnion<int>>("{\"Case2\":{\"value2\":2}}", _structConverterSerializerOptions))
				.Should()
				.Throw<JsonException>()
				.WithMessage(
					$"Not all parameters are present in json for union case \"Case2\" of union \"{typeof(TestStructUnion<int>).Name}\". Expected: 2, present: 1.");
		}

		[Fact]
		public void Read_IfUnionJsonContainsMoreData_ThrowException()
		{
			// Act and Assert

			FluentActions.Invoking(() => JsonSerializer.Deserialize<TestUnion<int>>("{\"Case3\":{\"value\":\"test\"},\"invalid\":\"yes\"}", _defaultConverterSerializerOptions))
				.Should()
				.Throw<JsonException>()
				.WithMessage(
					$"Unexpected end of union JSON. Token: \"{JsonTokenType.PropertyName}\", union: \"{typeof(TestUnion<int>).Name}\".");
			FluentActions.Invoking(() => JsonSerializer.Deserialize<TestStructUnion<int>>("{\"Case3\":{\"value\":\"test\"},\"invalid\":\"yes\"}", _defaultConverterSerializerOptions))
				.Should()
				.Throw<JsonException>()
				.WithMessage(
					$"Unexpected end of union JSON. Token: \"{JsonTokenType.PropertyName}\", union: \"{typeof(TestStructUnion<int>).Name}\".");
			FluentActions.Invoking(() => JsonSerializer.Deserialize<TestUnion<int>>("{\"Case3\":{\"value\":\"test\"},\"invalid\":\"yes\"}", _classConverterSerializerOptions))
				.Should()
				.Throw<JsonException>()
				.WithMessage(
					$"Unexpected end of union JSON. Token: \"{JsonTokenType.PropertyName}\", union: \"{typeof(TestUnion<int>).Name}\".");
			FluentActions.Invoking(() => JsonSerializer.Deserialize<TestStructUnion<int>>("{\"Case3\":{\"value\":\"test\"},\"invalid\":\"yes\"}", _structConverterSerializerOptions))
				.Should()
				.Throw<JsonException>()
				.WithMessage(
					$"Unexpected end of union JSON. Token: \"{JsonTokenType.PropertyName}\", union: \"{typeof(TestStructUnion<int>).Name}\".");
		}
	}
}