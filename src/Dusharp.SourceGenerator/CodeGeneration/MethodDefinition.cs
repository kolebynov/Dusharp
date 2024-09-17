using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Dusharp.CodeGeneration;

public sealed class MethodDefinition
{
	public Accessibility? Accessibility { get; init; }

	public required string ReturnType { get; init; }

	public required string Name { get; init; }

	public MethodModifier? MethodModifier { get; init; }

	public bool IsPartial { get; init; }

	public IReadOnlyList<MethodParameter> Parameters { get; init; } = [];

	public IReadOnlyList<string> GenericParameters { get; init; } = [];

	public Action<MethodDefinition, TypeDefinition, CodeWriter>? BodyWriter { get; init; }

	public bool HasParameters => Parameters.Count > 0;
}