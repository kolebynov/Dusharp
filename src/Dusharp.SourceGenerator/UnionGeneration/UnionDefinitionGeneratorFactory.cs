using System;
using Dusharp.CodeAnalyzing;

namespace Dusharp.UnionGeneration;

public sealed class UnionDefinitionGeneratorFactory : IUnionDefinitionGeneratorFactory
{
	public IUnionDefinitionGenerator Create(UnionInfo union) =>
		union.TypeInfo.Kind.Match(
			_ => (IUnionDefinitionGenerator)new ClassUnionDefinitionGenerator(union),
			_ => new StructUnionDefinitionGenerator(union),
			() => throw new ArgumentException("Can't create generator for unknown union type kind", nameof(union)));
}