using Dusharp.CodeAnalyzing;

namespace Dusharp.CodeGeneration;

public sealed class OperatorDefinition
{
	public required TypeName ReturnType { get; init; }

	public required string Name { get; init; }

	public IReadOnlyList<MethodParameter> Parameters { get; init; } = [];

	public required Action<OperatorDefinition, CodeWriter> BodyWriter { get; init; }
}