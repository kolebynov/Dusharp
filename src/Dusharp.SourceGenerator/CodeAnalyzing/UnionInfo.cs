using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Dusharp.CodeAnalyzing;

public sealed class UnionInfo
{
	public string Name { get; }

	public IReadOnlyList<UnionCaseInfo> Cases { get; }

	public IReadOnlyList<string> GenericParameters { get; }

	public INamedTypeSymbol TypeSymbol { get; }

	public UnionInfo(string name, IReadOnlyList<UnionCaseInfo> cases, IReadOnlyList<string> genericParameters,
		INamedTypeSymbol typeSymbol)
	{
		Name = name;
		Cases = cases;
		GenericParameters = genericParameters;
		TypeSymbol = typeSymbol;
	}
}