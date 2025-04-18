using Dusharp.SourceGenerator.CodeAnalyzing;

namespace Dusharp.SourceGenerator.UnionGeneration;

public interface IUnionDefinitionGeneratorFactory
{
	IUnionDefinitionGenerator Create(UnionInfo union);
}