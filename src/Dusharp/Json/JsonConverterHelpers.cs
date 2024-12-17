using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Dusharp.Json;

public static class JsonConverterHelpers
{
	public static readonly MethodInfo WriteStartObjectMethodInfo = typeof(Utf8JsonWriter).GetMethod(
		nameof(Utf8JsonWriter.WriteStartObject), BindingFlags.Public | BindingFlags.Instance, null,
		CallingConventions.HasThis, [], [])!;

	public static readonly MethodInfo WriteStartObjectWithPropertyMethodInfo = typeof(Utf8JsonWriter).GetMethod(
		nameof(Utf8JsonWriter.WriteStartObject), BindingFlags.Public | BindingFlags.Instance, null,
		CallingConventions.HasThis, [typeof(JsonEncodedText)], [])!;

	public static readonly MethodInfo WriteEndObjectMethodInfo = typeof(Utf8JsonWriter).GetMethod(
		nameof(Utf8JsonWriter.WriteEndObject), BindingFlags.Public | BindingFlags.Instance, null,
		CallingConventions.HasThis, [], [])!;

	public static readonly MethodInfo WriteStringValueMethodInfo = typeof(Utf8JsonWriter).GetMethod(
		nameof(Utf8JsonWriter.WriteStringValue), BindingFlags.Public | BindingFlags.Instance, null,
		CallingConventions.HasThis, [typeof(JsonEncodedText)], [])!;

	public static readonly PropertyInfo TokenTypePropertyInfo =
		typeof(Utf8JsonReader).GetProperty(nameof(Utf8JsonReader.TokenType), BindingFlags.Public | BindingFlags.Instance)!;

	public static readonly MethodInfo ReadMethodInfo =
		typeof(Utf8JsonReader).GetMethod(nameof(Utf8JsonReader.Read), BindingFlags.Public | BindingFlags.Instance)!;

	public static readonly MethodInfo WritePropertyGenericMethodInfo =
		GetDelegateMethodInfo(WriteProperty<int>).GetGenericMethodDefinition();

	public static readonly MethodInfo WriteEmptyObjectMethodInfo =
		GetDelegateMethodInfo(WriteEmptyObject);

	public static readonly MethodInfo ReadAndTokenIsPropertyNameMethodInfo =
		GetDelegateMethodInfo(ReadAndTokenIsPropertyName);

	public static readonly MethodInfo ValueTextEqualsMethodInfo = GetDelegateMethodInfo(ValueTextEquals);

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

	public static void BeforeRead(ref Utf8JsonReader reader, Type unionType)
	{
		if (reader.TokenType is not JsonTokenType.StartObject and not JsonTokenType.String)
		{
			throw new JsonException(
				$"""Invalid start token "{reader.TokenType}" when deserializing "{unionType.Name}" union. Expected "StartObject" or "String".""");
		}
	}

	public static void AfterRead(ref Utf8JsonReader reader, Type unionType)
	{
		if (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
		{
			throw new JsonException(
				$"""Unexpected end of union JSON. Token: "{reader.TokenType}", union: "{unionType.Name}".""");
		}
	}

	public static void WriteProperty<T>(Utf8JsonWriter writer, JsonEncodedText name, T value,
		JsonSerializerOptions options)
	{
		writer.WritePropertyName(name);
		JsonSerializer.Serialize(writer, value, options);
	}

	public static void WriteEmptyObject(Utf8JsonWriter writer)
	{
		writer.WriteStartObject();
		writer.WriteEndObject();
	}

	public static T? Deserialize<T>(ref Utf8JsonReader reader, JsonSerializerOptions options) =>
		JsonSerializer.Deserialize<T>(ref reader, options);

	public static bool ReadAndTokenIsPropertyName(ref Utf8JsonReader reader) =>
		reader.Read() && reader.TokenType == JsonTokenType.PropertyName;

	private static bool ValueTextEquals(ref Utf8JsonReader reader, byte[] utf8Name) =>
		reader.ValueTextEquals(utf8Name);

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ThrowInvalidCaseName(ref Utf8JsonReader reader, Type unionType) =>
		throw new JsonException($"""There is no case named "{reader.GetString()}" in union "{unionType.Name}".""");

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ThrowInvalidParameterlessCaseName(ref Utf8JsonReader reader, Type unionType) =>
		throw new JsonException($"""There is no parameterless case named "{reader.GetString()}" in union "{unionType.Name}".""");

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ThrowNotAllCaseParametersPresent(Type unionType, string caseName, int presentCount, int expectedCount) =>
		throw new JsonException($"""Not all parameters are present in json for union case "{caseName}" of union "{unionType.Name}". Expected: {expectedCount}, present: {presentCount}.""");

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ThrowInvalidUnionJsonObject(ref Utf8JsonReader reader) =>
		throw new JsonException($"""There is an invalid union JSON object. It must contain property with case name. There is a token "{reader.TokenType}".""");

	private static MethodInfo GetDelegateMethodInfo(Delegate @delegate) => @delegate.Method;
}