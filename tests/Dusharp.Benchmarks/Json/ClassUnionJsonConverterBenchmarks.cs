using System.Text.Json;
using BenchmarkDotNet.Attributes;

namespace Dusharp.Benchmarks.Json;

public class ClassUnionJsonConverterBenchmarks : BaseJsonConverterBenchmark
{
	private byte[] _serializedUnion = null!;

	[ParamsSource(nameof(GetClassUnionValues))]
	public TestUnion<int> ClassUnion { get; set; } = null!;

	public override void Setup()
	{
		base.Setup();
		_serializedUnion = JsonSerializer.SerializeToUtf8Bytes(ClassUnion, SerializerOptions);
	}

	[Benchmark]
	public void DefaultConverter_ClassUnion_Write()
	{
		Utf8JsonWriter.Reset();
		BufferWriter.ResetWrittenCount();
		DefaultUnionJsonConverter.Write(Utf8JsonWriter, ClassUnion, SerializerOptions);
	}

	[Benchmark]
	public void SpecializedConverter_ClassUnion_Write()
	{
		Utf8JsonWriter.Reset();
		BufferWriter.ResetWrittenCount();
		TestUnionJsonConverter.Write(Utf8JsonWriter, ClassUnion, SerializerOptions);
	}

	[Benchmark]
	public void DefaultConverter_ClassUnion_Read()
	{
		var reader = new Utf8JsonReader(_serializedUnion);
		reader.Read();
		DefaultUnionJsonConverter.Read(ref reader, typeof(TestUnion<int>), SerializerOptions);
	}

	[Benchmark]
	public void SpecializedConverter_ClassUnion_Read()
	{
		var reader = new Utf8JsonReader(_serializedUnion);
		reader.Read();
		TestUnionJsonConverter.Read(ref reader, typeof(TestUnion<int>), SerializerOptions);
	}
}