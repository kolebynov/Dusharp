using System.Linq;
using Dusharp.CodeAnalyzing;
using Dusharp.CodeGeneration;

namespace Dusharp.Extensions;

public static class UnionExtensions
{
	public static TypeName GetTypeName(this UnionInfo union) =>
		new(union.Name, union.TypeSymbol.TypeParameters.Select(x => x.Name).ToArray());
}