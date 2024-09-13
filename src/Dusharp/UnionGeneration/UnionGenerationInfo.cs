using System.Collections.Generic;
using System.Linq;
using Dusharp.CodeAnalyzing;
using Microsoft.CodeAnalysis;

namespace Dusharp.UnionGeneration;

public sealed class UnionGenerationInfo
{
	public string Name { get; }

	public string ClassName { get; }

	public IReadOnlyList<UnionCaseGenerationInfo> Cases { get; }

	public IReadOnlyList<string> GenericParameters { get; }

	public INamedTypeSymbol TypeSymbol { get; }

	public UnionGenerationInfo(UnionInfo unionInfo)
	{
		Name = unionInfo.Name;
		Cases = unionInfo.Cases.Select(x => new UnionCaseGenerationInfo(x)).ToArray();
		GenericParameters = unionInfo.GenericParameters;
		TypeSymbol = unionInfo.TypeSymbol;

		ClassName = GenericParameters.Count > 0 ? $"{Name}<{string.Join(", ", GenericParameters)}>" : Name;
	}
}