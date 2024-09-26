using Dusharp.CodeAnalyzing;

namespace Dusharp.UnionGeneration;

public sealed class UnionDefinitionGeneratorFactory : IUnionDefinitionGeneratorFactory
{
	public IUnionDefinitionGenerator Create(UnionInfo union) =>
		union.TypeSymbol.IsValueType switch
		{
			true => new StructUnionDefinitionGenerator(union),
			false => new ClassUnionDefinitionGenerator(union),
		};
}