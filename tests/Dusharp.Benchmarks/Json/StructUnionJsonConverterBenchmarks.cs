using System.Text.Json;
using BenchmarkDotNet.Attributes;

namespace Dusharp.Benchmarks.Json;

public class StructUnionJsonConverterBenchmarks : BaseJsonConverterBenchmark
{
	private byte[] _serializedUnion = null!;

	[ParamsSource(nameof(GetStructUnionValues))]
	public TestStructUnion<int> StructUnion { get; set; }

	public override void Setup()
	{
		base.Setup();
		_serializedUnion = JsonSerializer.SerializeToUtf8Bytes(StructUnion, SerializerOptions);
	}

	[Benchmark]
	public void DefaultConverter_StructUnion_Write()
	{
		Utf8JsonWriter.Reset();
		BufferWriter.ResetWrittenCount();
		DefaultUnionJsonConverter.Write(Utf8JsonWriter, StructUnion, SerializerOptions);
	}

	[Benchmark]
	public void SpecializedConverter_StructUnion_Write()
	{
		Utf8JsonWriter.Reset();
		BufferWriter.ResetWrittenCount();
		TestStructUnionJsonConverter.Write(Utf8JsonWriter, StructUnion, SerializerOptions);
	}

	[Benchmark]
	public void DefaultConverter_StructUnion_Read()
	{
		var reader = new Utf8JsonReader(_serializedUnion);
		reader.Read();
		DefaultUnionJsonConverter.Read(ref reader, typeof(TestStructUnion<int>), SerializerOptions);
	}

	[Benchmark]
	public void SpecializedConverter_StructUnion_Read()
	{
		var reader = new Utf8JsonReader(_serializedUnion);
		reader.Read();
		TestStructUnionJsonConverter.Read(ref reader, typeof(TestStructUnion<int>), SerializerOptions);
	}
}