using Dusharp.SourceGenerator.CodeAnalyzing;
using Microsoft.CodeAnalysis;

namespace Dusharp.SourceGenerator.CodeGeneration;

public sealed class FieldDefinition
{
	public Accessibility? Accessibility { get; init; }

	public bool IsStatic { get; init; }

	public bool IsReadOnly { get; init; }

	public required TypeName TypeName { get; init; }

	public required string Name { get; init; }

	public string? Initializer { get; init; }

	public IReadOnlyList<string> Attributes { get; init; } = [];
}