using System.Text.Json;

namespace Dusharp.Tests.Json.TestHelpers
{
	internal sealed class StjJsonSerializer : IJsonSerializer
	{
		private readonly JsonSerializerOptions _serializerOptions;

		public StjJsonSerializer(JsonSerializerOptions serializerOptions)
		{
			_serializerOptions = serializerOptions;
		}

		public string Serialize<T>(T obj) => JsonSerializer.Serialize(obj, _serializerOptions);

		public T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, _serializerOptions)!;
	}
}