using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Dusharp.CodeAnalyzing;
using Dusharp.CodeGeneration;
using Dusharp.JsonConverterGeneration;
using Dusharp.UnionGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dusharp;

[Generator]
public sealed class UnionSourceGenerator : IIncrementalGenerator
{
	private const string UnionAttributeTypeName = "Dusharp.UnionAttribute";
	private const string UnionCaseAttributeTypeName = "Dusharp.UnionCaseAttribute";
	private const string GenerateJsonConverterAttributeTypeName = "Dusharp.Json.GenerateJsonConverterAttribute";

	private static readonly UnionCodeGenerator UnionCodeGenerator = new(new TypeCodeWriter(), new UnionDefinitionGeneratorFactory());
	private static readonly JsonConverterGenerator JsonConverterGenerator = new(new TypeCodeWriter());

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var unionsProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
			UnionAttributeTypeName,
			(node, _) => node is ClassDeclarationSyntax or StructDeclarationSyntax,
			(ctx, _) => (INamedTypeSymbol)ctx.TargetSymbol);
		var attributesProvider = context.CompilationProvider
			.Select((x, _) => (x.GetTypeByMetadataName(UnionCaseAttributeTypeName)!, x.GetTypeByMetadataName(GenerateJsonConverterAttributeTypeName)!));

		context.RegisterSourceOutput(
			unionsProvider.Combine(attributesProvider),
			(ctx, tuple) =>
			{
				var (typeSymbol, (unionCaseAttributeSymbol, generateJsonConverterAttributeSymbol)) = tuple;

				try
				{
					var unionInfo = UnionInfoCollector.Collect(typeSymbol, unionCaseAttributeSymbol!);
					if (unionInfo.Cases.Count == 0)
					{
						return;
					}

					var typeFileName = typeSymbol.ToDisplayString().Replace('<', '(').Replace('>', ')');
					ctx.AddSource($"{typeFileName}.Dusharp.g.cs", UnionCodeGenerator.GenerateUnion(unionInfo));

					if (typeSymbol.GetAttributes()
					    .Any(x => generateJsonConverterAttributeSymbol!.Equals(x.AttributeClass, SymbolEqualityComparer.Default)))
					{
						ctx.AddSource($"{typeFileName}.Dusharp.Json.g.cs", JsonConverterGenerator.GenerateJsonConverter(unionInfo));
					}
				}
				catch (Exception e)
				{
					ctx.ReportDiagnostic(Diagnostic.Create(
						new DiagnosticDescriptor("DU0001", "Exception", e.ToString().Replace("\r", string.Empty).Replace('\n', ' '), "error", DiagnosticSeverity.Error, true),
						null));
				}
			});
	}
}