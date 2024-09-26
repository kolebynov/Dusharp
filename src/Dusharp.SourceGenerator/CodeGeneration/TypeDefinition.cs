using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Dusharp.CodeGeneration;

public sealed record class TypeDefinition
{
	private TypeName? _typeName;

	public Accessibility? Accessibility { get; init; }

	public bool IsPartial { get; init; }

	public required string Name { get; init; }

	public required TypeKind Kind { get; init; }

	public string FullName => (_typeName ??= new TypeName(Name, GenericParameters)).FullName;

	public IReadOnlyList<string> Attributes { get; init; } = [];

	public IReadOnlyList<string> GenericParameters { get; init; } = [];

	public IReadOnlyList<string> InheritedTypes { get; init; } = [];

	public IReadOnlyList<FieldDefinition> Fields { get; init; } = [];

	public IReadOnlyList<ConstructorDefinition> Constructors { get; init; } = [];

	public IReadOnlyList<MethodDefinition> Methods { get; init; } = [];

	public IReadOnlyList<OperatorDefinition> Operators { get; init; } = [];

	public IReadOnlyList<TypeDefinition> NestedTypes { get; init; } = [];
}