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
	private static void ThrowArgumentNull(string paramName) => throw new ArgumentNullException(paramName);
}