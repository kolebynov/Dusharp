using Microsoft.CodeAnalysis;
using TypeInfo = Dusharp.SourceGenerator.CodeAnalyzing.TypeInfo;

namespace Dusharp.SourceGenerator.CodeGeneration;

public sealed record class TypeDefinition
{
	private string? _nameWithoutGenerics;

	public Accessibility? Accessibility { get; init; }

	public bool IsPartial { get; init; }

	public required string Name { get; init; }

	public string NameWithoutGenerics
	{
		get
		{
			if (_nameWithoutGenerics != null)
			{
				return _nameWithoutGenerics;
			}

			var genericStart = Name.IndexOf('<');
			if (genericStart < 0)
			{
				return _nameWithoutGenerics = Name;
			}

			return _nameWithoutGenerics = Name[..genericStart];
		}
	}

	public required TypeKind Kind { get; init; }

	public IReadOnlyList<string> Attributes { get; init; } = [];

	public IReadOnlyList<TypeInfo> InheritedTypes { get; init; } = [];

	public IReadOnlyList<FieldDefinition> Fields { get; init; } = [];

	public IReadOnlyList<PropertyDefinition> Properties { get; init; } = [];

	public IReadOnlyList<ConstructorDefinition> Constructors { get; init; } = [];

	public IReadOnlyList<MethodDefinition> Methods { get; init; } = [];

	public IReadOnlyList<OperatorDefinition> Operators { get; init; } = [];

	public IReadOnlyList<TypeDefinition> NestedTypes { get; init; } = [];
}