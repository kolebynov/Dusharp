using System;
using System.Collections.Generic;

namespace Dusharp.CodeGeneration;

public sealed class OperatorDefinition
{
	public required string ReturnType { get; init; }

	public required string Name { get; init; }

	public IReadOnlyList<MethodParameter> Parameters { get; init; } = [];

	public required Action<OperatorDefinition, TypeDefinition, CodeWriter> BodyWriter { get; init; }
}