using Newtonsoft.Json;

namespace Dusharp.Tests.Json.TestHelpers
{
	internal sealed class NewtonsoftJsonSerializer : IJsonSerializer
	{
		private readonly JsonSerializerSettings _serializerSettings;

		public NewtonsoftJsonSerializer(JsonSerializerSettings serializerSettings)
		{
			_serializerSettings = serializerSettings;
		}

		public string Serialize<T>(T obj) => JsonConvert.SerializeObject(obj, _serializerSettings);

		public T Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json, _serializerSettings)!;
	}
}