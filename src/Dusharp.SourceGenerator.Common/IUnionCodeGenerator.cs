using Dusharp.SourceGenerator.Common.CodeAnalyzing;
using Microsoft.CodeAnalysis;

namespace Dusharp.SourceGenerator.Common;

public interface IUnionCodeGenerator
{
	public string Name { get; }

	public string? GenerateCode(UnionInfo unionInfo, INamedTypeSymbol unionTypeSymbol);
}