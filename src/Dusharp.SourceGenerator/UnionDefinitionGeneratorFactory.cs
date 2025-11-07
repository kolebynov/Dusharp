using Dusharp.SourceGenerator.Common.CodeAnalyzing;

namespace Dusharp.SourceGenerator;

public sealed class UnionDefinitionGeneratorFactory : IUnionDefinitionGeneratorFactory
{
	public IUnionDefinitionGenerator Create(UnionInfo union) =>
		union.TypeInfo.Kind switch
		{
			TypeInfo.TypeKind.ReferenceType => new ClassUnionDefinitionGenerator(union),
			TypeInfo.TypeKind.ValueType => new StructUnionDefinitionGenerator(union),
			_ => throw new ArgumentException("Can't create generator for unknown union type kind", nameof(union)),
		};
}