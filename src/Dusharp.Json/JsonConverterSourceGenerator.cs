using Dusharp.SourceGenerator.Common;
using Dusharp.SourceGenerator.Common.CodeGeneration;
using Microsoft.CodeAnalysis;

namespace Dusharp.Json;

[Generator]
public sealed class JsonConverterSourceGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context) =>
		UnionSourceGeneratorBootstrapper.Bootstrap(context, new JsonConverterGenerator(new TypeCodeWriter()));
}