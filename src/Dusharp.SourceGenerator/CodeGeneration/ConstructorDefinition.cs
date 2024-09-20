using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Dusharp.CodeGeneration;

public sealed class ConstructorDefinition
{
	public Accessibility? Accessibility { get; init; }

	public bool IsStatic { get; init; }

	public IReadOnlyList<MethodParameter> Parameters { get; init; } = [];

	public required Action<ConstructorDefinition, CodeWriter> BodyWriter { get; init; }
}