using Dusharp.CodeAnalyzing;

namespace Dusharp.CodeGeneration;

public readonly record struct MethodParameter(TypeName TypeName, string Name, MethodParameterModifier? Modifier = null);