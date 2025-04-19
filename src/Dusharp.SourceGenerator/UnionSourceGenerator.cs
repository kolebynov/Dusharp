using Dusharp.SourceGenerator.Common;
using Dusharp.SourceGenerator.Common.CodeGeneration;
using Microsoft.CodeAnalysis;

namespace Dusharp.SourceGenerator;

[Generator]
public sealed class UnionSourceGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context) =>
		UnionSourceGeneratorBootstrapper.Bootstrap(
			context, new UnionCodeGenerator(new TypeCodeWriter(), new UnionDefinitionGeneratorFactory()));
}