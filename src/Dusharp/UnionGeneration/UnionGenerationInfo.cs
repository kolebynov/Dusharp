using System.Collections.Generic;
using System.Linq;
using Dusharp.CodeAnalyzing;
using Microsoft.CodeAnalysis;

namespace Dusharp.UnionGeneration;

public sealed class UnionGenerationInfo
{
	public string Name { get; }

	public IReadOnlyList<UnionCaseGenerationInfo> Cases { get; }

	public INamedTypeSymbol TypeSymbol { get; }

	public UnionGenerationInfo(UnionInfo unionInfo)
	{
		Name = unionInfo.Name;
		Cases = unionInfo.Cases.Select(x => new UnionCaseGenerationInfo(x)).ToArray();
		TypeSymbol = unionInfo.TypeSymbol;
	}
}