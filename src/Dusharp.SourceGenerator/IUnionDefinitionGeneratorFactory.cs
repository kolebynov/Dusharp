using Dusharp.SourceGenerator.Common.CodeAnalyzing;

namespace Dusharp.SourceGenerator;

public interface IUnionDefinitionGeneratorFactory
{
	IUnionDefinitionGenerator Create(UnionInfo union);
}