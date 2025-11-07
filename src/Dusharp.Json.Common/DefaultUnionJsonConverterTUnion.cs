#if NET8_0_OR_GREATER

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dusharp.Json;

#pragma warning disable SA1649
public sealed class DefaultUnionJsonConverter<TUnion> : JsonConverter<TUnion>
	where TUnion : IUnion<TUnion>
{
	public override bool CanConvert(Type typeToConvert) =>
		typeToConvert == typeof(TUnion) || typeToConvert.BaseType == typeof(TUnion);

	public override TUnion? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var description = TUnion.UnionDescription;
		JsonConverterHelpers.BeforeRead(ref reader, typeof(TUnion));

	}

	public override void Write(Utf8JsonWriter writer, TUnion value, JsonSerializerOptions options)
	{
		var caseName = value.CaseName;
		var description = TUnion.UnionDescription;
		var caseDescription = description.Cases.FirstOrDefault(x => x.Name == caseName);
		if (caseDescription == null)
		{
			JsonConverterHelpers.ThrowUnionInInvalidState(typeof(TUnion), nameof(value));
			return;
		}

		if (caseDescription.Parameters.Count == 0)
		{
			writer.WriteStringValue(caseName);
			return;
		}

		Span<nint> caseParametersPtrs = stackalloc nint[caseDescription.Parameters.Count];
		var caseParameters = MemoryMarshal.CreateSpan(
			ref Unsafe.As<nint, object?>(ref MemoryMarshal.GetReference(caseParametersPtrs)),
			caseDescription.Parameters.Count);
		value.GetCaseParameters(caseParameters);
		writer.WriteStartObject();
		writer.WriteStartObject(caseDescription.Name);
		foreach (var (caseParameterDescription, index) in caseDescription.Parameters.Select((x, i) => (x, i)))
		{
			JsonConverterHelpers.WriteProperty(writer, JsonEncodedText.Encode(caseParameterDescription.Name), caseParameters[index], options);
		}

		writer.WriteEndObject();
		writer.WriteEndObject();
	}
}
#endif