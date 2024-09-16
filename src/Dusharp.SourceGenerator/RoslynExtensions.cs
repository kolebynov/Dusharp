using System;
using Microsoft.CodeAnalysis;

namespace Dusharp;

internal static class RoslynExtensions
{
	public static string ToCodeString(this Accessibility accessibility) => accessibility switch
	{
		Accessibility.Public => "public",
		Accessibility.Internal => "internal",
		Accessibility.Protected => "protected",
		Accessibility.ProtectedAndInternal => "protected internal",
		Accessibility.Private => "private",
		_ => throw new ArgumentOutOfRangeException(nameof(accessibility), "Invalid type accessibility"),
	};

	public static string ToCodeString(this TypeKind typeKind) => typeKind switch
	{
		TypeKind.Struct => "struct",
		TypeKind.Class => "class",
		_ => throw new ArgumentOutOfRangeException(nameof(typeKind), "Invalid type kind"),
	};
}
