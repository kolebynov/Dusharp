using Dusharp.SourceGenerator.CodeAnalyzing;

namespace Dusharp.SourceGenerator.CodeGeneration;

public readonly record struct MethodParameter(TypeName TypeName, string Name, MethodParameterModifier? Modifier = null);