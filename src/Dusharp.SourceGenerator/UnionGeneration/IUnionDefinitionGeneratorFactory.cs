using Dusharp.CodeAnalyzing;

namespace Dusharp.UnionGeneration;

public interface IUnionDefinitionGeneratorFactory
{
	IUnionDefinitionGenerator Create(UnionInfo union);
}