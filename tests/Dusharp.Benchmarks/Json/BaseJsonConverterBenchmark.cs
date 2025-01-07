using System.Buffers;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Dusharp.Json;

namespace Dusharp.Benchmarks.Json;

public abstract class BaseJsonConverterBenchmark
{
	[GlobalSetup]
	public virtual void Setup()
	{
		DefaultUnionJsonConverter = new DefaultUnionJsonConverter();
		TestUnionJsonConverter = new TestUnion<int>.JsonConverter();
		TestStructUnionJsonConverter = new TestStructUnion<int>.JsonConverter();
		SerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.General)
		{
			Converters = { DefaultUnionJsonConverter },
		};
		BufferWriter = new ArrayBufferWriter<byte>(1024);
		Utf8JsonWriter = new Utf8JsonWriter(BufferWriter);
	}

	public static IEnumerable<TestUnion<int>> GetClassUnionValues()
	{
		yield return TestUnion<int>.Case1();
		yield return TestUnion<int>.Case4(20, "stringValue", 10, DateTime.UnixEpoch);
	}

	public static IEnumerable<TestStructUnion<int>> GetStructUnionValues()
	{
		yield return TestStructUnion<int>.Case1();
		yield return TestStructUnion<int>.Case4(20, "stringValue", 10, DateTime.UnixEpoch);
	}

	protected DefaultUnionJsonConverter DefaultUnionJsonConverter { get; private set; } = null!;

	protected TestUnion<int>.JsonConverter TestUnionJsonConverter { get; private set; } = null!;

	protected TestStructUnion<int>.JsonConverter TestStructUnionJsonConverter { get; private set; } = null!;

	protected JsonSerializerOptions SerializerOptions { get; private set; } = null!;

	protected ArrayBufferWriter<byte> BufferWriter { get; private set; } = null!;

	protected Utf8JsonWriter Utf8JsonWriter { get; private set; } = null!;
}