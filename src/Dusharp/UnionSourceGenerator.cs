﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dusharp;

[Generator]
public sealed class UnionSourceGenerator : IIncrementalGenerator
{
	private static readonly Type UnionAttributeType = typeof(UnionAttribute);
	private static readonly Type UnionCaseAttributeType = typeof(UnionCaseAttribute);

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(ctx =>
		{
			var currentAssembly = Assembly.GetExecutingAssembly();
			foreach (var fileName in currentAssembly.GetManifestResourceNames().Where(x => x.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)))
			{
				using var streamReader = new StreamReader(currentAssembly.GetManifestResourceStream(fileName)!);
				ctx.AddSource(fileName, $"// <auto-generated> This file has been auto generated. </auto-generated>\n{streamReader.ReadToEnd()}");
			}
		});

		var unionsProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
			UnionAttributeType.FullName!,
			(node, _) => node is ClassDeclarationSyntax,
			(ctx, _) => (INamedTypeSymbol)ctx.TargetSymbol);
		var caseAttributeProvider = context.CompilationProvider
			.Select((x, _) => x.GetTypeByMetadataName(UnionCaseAttributeType.FullName!));

		context.RegisterSourceOutput(unionsProvider.Combine(caseAttributeProvider), (ctx, tuple) =>
		{
			var (typeSymbol, unionCaseAttributeSymbol) = tuple;

			var unionInfo = UnionInfoCollector.Collect(typeSymbol, unionCaseAttributeSymbol!);
			var unionCode = UnionCodeGenerator.GenerateClassUnion(unionInfo);
			if (unionCode != null)
			{
				ctx.AddSource($"{typeSymbol}.Union.g.cs", unionCode);
			}
		});
	}
}