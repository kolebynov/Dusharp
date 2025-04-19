using Dusharp.SourceGenerator.Common.CodeAnalyzing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dusharp.SourceGenerator.Common;

public static class UnionSourceGeneratorBootstrapper
{
	private const string UnionAttributeTypeName = "Dusharp.UnionAttribute";
	private const string UnionCaseAttributeTypeName = "Dusharp.UnionCaseAttribute";

	public static void Bootstrap(
		IncrementalGeneratorInitializationContext context, IUnionCodeGenerator unionCodeGenerator)
	{
		var unionsProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
			UnionAttributeTypeName,
			(node, _) => node is ClassDeclarationSyntax or StructDeclarationSyntax,
			(ctx, _) => (INamedTypeSymbol)ctx.TargetSymbol);
		var attributesProvider = context.CompilationProvider
			.Select((x, _) => x.GetTypeByMetadataName(UnionCaseAttributeTypeName)!);

		context.RegisterSourceOutput(
			unionsProvider.Combine(attributesProvider),
			(ctx, tuple) =>
			{
				var (typeSymbol, unionCaseAttributeSymbol) = tuple;

				try
				{
					var unionInfo = UnionInfoCollector.Collect(typeSymbol, unionCaseAttributeSymbol!);
					if (unionInfo.Cases.Count == 0)
					{
						return;
					}

					var code = unionCodeGenerator.GenerateCode(unionInfo, typeSymbol);
					if (string.IsNullOrEmpty(code))
					{
						return;
					}

					var typeFileName = typeSymbol.ToDisplayString().Replace('<', '(').Replace('>', ')');
					ctx.AddSource($"{typeFileName}.Dusharp.{unionCodeGenerator.Name}.g.cs", code!);
				}
				catch (Exception e)
				{
					ReportException(ctx, e);
				}
			});
	}

	private static void ReportException(SourceProductionContext context, Exception e, string? message = null)
	{
		var exceptionString = e.ToString().Replace("\r", string.Empty).Replace('\n', ' ');

		context.ReportDiagnostic(Diagnostic.Create(
			new DiagnosticDescriptor("DU0001", "Exception", string.IsNullOrEmpty(message) ? exceptionString : $"{message}: {exceptionString}", "error", DiagnosticSeverity.Error, true),
			null));
	}
}