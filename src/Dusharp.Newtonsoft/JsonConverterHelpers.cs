using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace Dusharp.Newtonsoft;

internal static class JsonConverterHelpers
{
	public static readonly MethodInfo WriteStartObjectMethodInfo = typeof(JsonWriter).GetMethod(
		nameof(JsonWriter.WriteStartObject), BindingFlags.Public | BindingFlags.Instance, null,
		CallingConventions.HasThis, [], [])!;

	public static readonly MethodInfo WriteStartObjectWithPropertyMethodInfo = GetDelegateMethodInfo(WriteStartObjectWithProperty);

	public static readonly MethodInfo WriteEndObjectMethodInfo = typeof(JsonWriter).GetMethod(
		nameof(JsonWriter.WriteEndObject), BindingFlags.Public | BindingFlags.Instance, null,
		CallingConventions.HasThis, [], [])!;

	public static readonly MethodInfo WriteStringValueMethodInfo = typeof(JsonWriter).GetMethod(
		nameof(JsonWriter.WriteValue), BindingFlags.Public | BindingFlags.Instance, null,
		CallingConventions.HasThis, [typeof(string)], [])!;

	public static readonly PropertyInfo TokenTypePropertyInfo =
		typeof(JsonReader).GetProperty(nameof(JsonReader.TokenType), BindingFlags.Public | BindingFlags.Instance)!;

	public static readonly MethodInfo ReadMethodInfo =
		typeof(JsonReader).GetMethod(nameof(JsonReader.Read), BindingFlags.Public | BindingFlags.Instance)!;

	public static readonly MethodInfo WritePropertyGenericMethodInfo =
		GetDelegateMethodInfo(WriteProperty<int>).GetGenericMethodDefinition();

	public static readonly MethodInfo ReadAndTokenIsPropertyNameMethodInfo =
		GetDelegateMethodInfo(ReadAndTokenIsPropertyName);

	public static readonly MethodInfo CurrentTokenEqualsMethodInfo = GetDelegateMethodInfo(CurrentTokenEquals);

	public static readonly MethodInfo DeserializeGenericMethodInfo =
		GetDelegateMethodInfo(Deserialize<int>).GetGenericMethodDefinition();

	public static readonly MethodInfo ThrowInvalidCaseNameMethodInfo =
		GetDelegateMethodInfo(ThrowInvalidCaseName);

	public static readonly MethodInfo ThrowInvalidParameterlessCaseNameMethodInfo =
		GetDelegateMethodInfo(ThrowInvalidParameterlessCaseName);

	public static readonly MethodInfo ThrowNotAllCaseParametersPresentMethodInfo =
		GetDelegateMethodInfo(ThrowNotAllCaseParametersPresent);

	public static readonly MethodInfo ThrowInvalidUnionJsonObjectMethodInfo =
		GetDelegateMethodInfo(ThrowInvalidUnionJsonObject);

	public static readonly MethodInfo ThrowUnionInInvalidStateMethodInfo =
		GetDelegateMethodInfo(ThrowUnionInInvalidState);

	public static void BeforeRead(JsonReader reader, Type unionType)
	{
		if (reader.TokenType is not JsonToken.StartObject and not JsonToken.String)
		{
			throw new JsonSerializationException(
				$"""Invalid start token "{reader.TokenType}" when deserializing "{unionType.Name}" union. Expected "StartObject" or "String".""");
		}
	}

	public static void AfterRead(JsonReader reader, bool needRead)
	{
		if (needRead)
		{
			reader.Read();
		}
	}

	public static void WriteProperty<T>(JsonWriter writer, string name, T value,
		JsonSerializer serializer)
	{
		writer.WritePropertyName(name);
		serializer.Serialize(writer, value);
	}

	public static void WriteStartObjectWithProperty(JsonWriter writer, string name)
	{
		writer.WritePropertyName(name);
		writer.WriteStartObject();
	}

	public static T? Deserialize<T>(JsonReader reader, JsonSerializer serializer) =>
		serializer.Deserialize<T>(reader);

	public static bool ReadAndTokenIsPropertyName(JsonReader reader) =>
		reader.Read() && reader.TokenType == JsonToken.PropertyName;

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ThrowInvalidCaseName(JsonReader reader, Type unionType) =>
		throw new JsonSerializationException($"""There is no case named "{reader.Value}" in union "{unionType.Name}".""");

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ThrowInvalidParameterlessCaseName(JsonReader reader, Type unionType) =>
		throw new JsonSerializationException($"""There is no parameterless case named "{reader.Value}" in union "{unionType.Name}".""");

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ThrowNotAllCaseParametersPresent(Type unionType, string caseName, int presentCount, int expectedCount) =>
		throw new JsonSerializationException($"""Not all parameters are present in json for union case "{caseName}" of union "{unionType.Name}". Expected: {expectedCount}, present: {presentCount}.""");

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ThrowInvalidUnionJsonObject(JsonReader reader) =>
		throw new JsonSerializationException($"""There is an invalid union JSON object. It must contain property with case name. There is a token "{reader.TokenType}".""");

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ThrowUnionInInvalidState(Type unionType, string paramName) =>
		throw new ArgumentException($"Failed to serialize union {unionType.Name}. It's in invalid state (probably a struct default value).", paramName);

	private static bool CurrentTokenEquals(JsonReader reader, string value) =>
		reader.Value is string text && text == value;

	private static MethodInfo GetDelegateMethodInfo(Delegate @delegate) => @delegate.Method;
}