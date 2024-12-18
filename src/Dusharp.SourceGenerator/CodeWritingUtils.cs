using System;
using TypeInfo = Dusharp.CodeAnalyzing.TypeInfo;

namespace Dusharp;

public static class CodeWritingUtils
{
	public static void WriteSuppressWarning(this CodeWriter codeWriter, string checkId,
		string justification, bool useAttribute = true)
	{
		codeWriter.AppendLine(
			useAttribute
				? $"[global::System.Diagnostics.CodeAnalysis.SuppressMessage(\"\", \"{checkId}\", Justification = \"{justification}\")]"
				: $"#pragma warning disable {checkId} // {justification}");
	}

	public static void WriteContainingBlocks(TypeInfo typeInfo, CodeWriter codeWriter,
		Action<CodeWriter> innerBlockWriter)
	{
		if (typeInfo.ContainingType is { } containingType)
		{
			WriteContainingBlocks(containingType, codeWriter,
				writer =>
				{
					writer.AppendLine($"partial {containingType.Kind.ToCodeString()} {containingType.Name}");
					using var typeBodyBlock = writer.NewBlock();
					innerBlockWriter(typeBodyBlock);
				});
			return;
		}

		if (string.IsNullOrEmpty(typeInfo.Namespace))
		{
			innerBlockWriter(codeWriter);
		}
		else
		{
			codeWriter.AppendLine($"namespace {typeInfo.Namespace}");
			using var namespaceBlock = codeWriter.NewBlock();
			innerBlockWriter(namespaceBlock);
		}
	}
}