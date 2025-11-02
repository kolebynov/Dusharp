namespace Dusharp.Tests.Json.TestHelpers
{
	public interface IJsonSerializer
	{
		string Serialize<T>(T obj);

		T Deserialize<T>(string json);
	}
}