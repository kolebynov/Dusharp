using Microsoft.CodeAnalysis;

namespace Dusharp.CodeAnalyzing;

public readonly struct UnionCaseParameterInfo
{
	public string Name { get; }

	public ITypeSymbol Type { get; }

	public string TypeName { get; }

	public UnionCaseParameterInfo(string name, ITypeSymbol type)
	{
		Name = name;
		Type = type;
		TypeName = type.ToString();
	}
}