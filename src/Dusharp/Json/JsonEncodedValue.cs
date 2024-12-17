using System.Text;
using System.Text.Json;

namespace Dusharp.Json;

public readonly struct JsonEncodedValue
{
	public JsonEncodedText EncodedValue { get; }

	public byte[] Utf8Value { get; }

	public JsonEncodedValue(string value)
	{
		Utf8Value = Encoding.UTF8.GetBytes(value);
		EncodedValue = JsonEncodedText.Encode(Utf8Value);
	}

	public JsonEncodedValue(JsonEncodedText encodedValue, byte[] utf8Value)
	{
		EncodedValue = encodedValue;
		Utf8Value = utf8Value;
	}
}