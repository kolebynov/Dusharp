using System.Linq;
using Microsoft.CodeAnalysis;

namespace Dusharp.CodeAnalyzing;

public static class UnionInfoCollector
{
	public static UnionInfo Collect(INamedTypeSymbol unionClassSymbol, INamedTypeSymbol unionCaseAttributeSymbol)
	{
		var unionCases = unionClassSymbol.GetMembers()
			.OfType<IMethodSymbol>()
			.Where(methodSymbol => methodSymbol.GetAttributes()
				.Any(attr =>
					attr.AttributeClass?.Equals(unionCaseAttributeSymbol, SymbolEqualityComparer.Default) ??
					false))
			.Select(x => new UnionCaseInfo(
				x.Name,
				x.Parameters.Select(y => new UnionCaseParameterInfo(y.Name, y.Type)).ToArray()))
			.ToArray();

		return new UnionInfo(unionClassSymbol.Name, unionCases, unionClassSymbol);
	}
}