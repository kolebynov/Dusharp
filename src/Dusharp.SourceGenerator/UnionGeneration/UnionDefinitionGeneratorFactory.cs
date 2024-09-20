using Dusharp.CodeAnalyzing;

namespace Dusharp.UnionGeneration;

public sealed class UnionDefinitionGeneratorFactory : IUnionDefinitionGeneratorFactory
{
	public IUnionDefinitionGenerator Create(UnionInfo union) =>
		new ClassUnionDefinitionGenerator(union);
}