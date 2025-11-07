using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Dusharp;

public static class ExceptionUtils
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ThrowIfNull<T>(this T? value, string paramName)
		where T : class
	{
		if (value == null)
		{
			ThrowArgumentNull(paramName);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowUnionInInvalidState() =>
		throw new InvalidOperationException("Union in invalid state.");

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowInvalidParametersCount(string caseName, int expectedParameters, int actualParameters, string paramName) =>
		throw new ArgumentException($"Union case {caseName} has {expectedParameters} parameters, but {paramName} contains {actualParameters}", paramName);

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowCaseDoesNotExist(string caseName, string unionName, string paramName) =>
		throw new ArgumentException($"Union case {caseName} doesn't exist in union {unionName}", paramName);

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	private static void ThrowArgumentNull(string paramName) => throw new ArgumentNullException(paramName);
}