using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Dusharp;

public sealed class UnionInfo
{
	public string Name { get; }

	public IReadOnlyList<UnionCaseInfo> Cases { get; }

	public INamedTypeSymbol TypeSymbol { get; }

	public UnionInfo(string name, IReadOnlyList<UnionCaseInfo> cases, INamedTypeSymbol typeSymbol)
	{
		Name = name;
		Cases = cases;
		TypeSymbol = typeSymbol;
	}
}