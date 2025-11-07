using Dusharp.SourceGenerator.Common.CodeGeneration;

namespace Dusharp.SourceGenerator.Common.Extensions;

public static class CodeExtensions
{
	public static string ToCodeString(this MethodModifier methodModifier) => methodModifier switch
	{
		MethodModifier.Static => "static",
		MethodModifier.Abstract => "abstract",
		MethodModifier.Virtual => "virtual",
		MethodModifier.Override => "override",
		_ => throw new ArgumentOutOfRangeException(nameof(methodModifier), methodModifier, null),
	};

	public static string ToCodeString(this MethodParameterModifier methodParameterModifier) => methodParameterModifier switch
	{
		MethodParameterModifier.In => "in",
		MethodParameterModifier.Ref => "ref",
		MethodParameterModifier.Out => "out",
		_ => throw new ArgumentOutOfRangeException(nameof(methodParameterModifier), methodParameterModifier, null),
	};
}