using System;
using Microsoft.CodeAnalysis;

namespace Dusharp;

public static class CodeWritingUtils
{
	public static CodeWriter WriteOuterBlocks(INamedTypeSymbol typeSymbol, Action<CodeWriter> innerBlockWriter)
	{
		var codeWriter = new CodeWriter();
		WriteOuterBlocks(typeSymbol, codeWriter, innerBlockWriter);
		return codeWriter;
	}

	private static void WriteOuterBlocks(INamedTypeSymbol typeSymbol, CodeWriter codeWriter,
		Action<CodeWriter> innerBlockWriter)
	{
		if (typeSymbol.ContainingType != null)
		{
			WriteOuterBlocks(typeSymbol.ContainingType, codeWriter,
				writer =>
				{
					var containingType = typeSymbol.ContainingType;
					writer.AppendLine($"partial {containingType.TypeKind.ToCodeString()} {containingType.Name}");
					using var typeBodyBlock = writer.NewBlock();
					innerBlockWriter(typeBodyBlock);
				});
			return;
		}

		if (typeSymbol.ContainingNamespace?.IsGlobalNamespace ?? true)
		{
			innerBlockWriter(codeWriter);
		}
		else
		{
			codeWriter.AppendLine($"namespace {typeSymbol.ContainingNamespace}");
			using var namespaceBlock = codeWriter.NewBlock();
			innerBlockWriter(namespaceBlock);
		}
	}
}