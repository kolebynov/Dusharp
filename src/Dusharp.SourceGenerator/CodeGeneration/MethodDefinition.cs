using Dusharp.CodeAnalyzing;
using Microsoft.CodeAnalysis;

namespace Dusharp.CodeGeneration;

public sealed record class MethodDefinition
{
	public Accessibility? Accessibility { get; init; }

	public required TypeName ReturnType { get; init; }

	public required string Name { get; init; }

	public MethodModifier? MethodModifier { get; init; }

	public bool IsPartial { get; init; }

	public IReadOnlyList<MethodParameter> Parameters { get; init; } = [];

	public IReadOnlyList<string> GenericParameters { get; init; } = [];

	public Action<MethodDefinition, CodeWriter>? BodyWriter { get; init; }
}