namespace Dusharp.CodeGeneration;

public readonly record struct MethodParameter(string TypeName, string Name, MethodParameterModifier? Modifier = null);