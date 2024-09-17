using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Dusharp.CodeGeneration;

public sealed class TypeDefinition
{
	private string? _fullName;

	public Accessibility? Accessibility { get; init; }

	public bool IsPartial { get; init; }

	public required string Name { get; init; }

	public required TypeKind Kind { get; init; }

	public string FullName => _fullName ??= GetFullName();

	public IReadOnlyList<string> GenericParameters { get; init; } = [];

	public IReadOnlyList<string> InheritedTypes { get; init; } = [];

	public IReadOnlyList<FieldDefinition> Fields { get; init; } = [];

	public IReadOnlyList<ConstructorDefinition> Constructors { get; init; } = [];

	public IReadOnlyList<MethodDefinition> Methods { get; init; } = [];

	public IReadOnlyList<OperatorDefinition> Operators { get; init; } = [];

	public IReadOnlyList<TypeDefinition> NestedTypes { get; init; } = [];

	private string GetFullName()
	{
		var genericParametersStr = GenericParameters.Count > 0
			? $"<{string.Join(", ", GenericParameters)}>"
			: string.Empty;
		return $"{Name}{genericParametersStr}";
	}
}