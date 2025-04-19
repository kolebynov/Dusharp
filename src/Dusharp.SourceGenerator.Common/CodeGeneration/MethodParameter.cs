using Dusharp.SourceGenerator.Common.CodeAnalyzing;

namespace Dusharp.SourceGenerator.Common.CodeGeneration;

public readonly record struct MethodParameter(TypeName TypeName, string Name, MethodParameterModifier? Modifier = null);