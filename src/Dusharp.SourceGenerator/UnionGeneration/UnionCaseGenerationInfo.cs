using System;
using System.Collections.Generic;
using System.Linq;
using Dusharp.CodeAnalyzing;

namespace Dusharp.UnionGeneration;

public sealed class UnionCaseGenerationInfo
{
	public string Name { get; }

	public IReadOnlyList<UnionCaseParameterInfo> Parameters { get; }

	public string ClassName { get; }

	public string ClassNameCamelCase { get; }

	public bool HasParameters => Parameters.Count > 0;

	public UnionCaseGenerationInfo(UnionCaseInfo unionCaseInfo)
	{
		Name = unionCaseInfo.Name;
		Parameters = unionCaseInfo.Parameters;
		ClassName = $"{Name}Case";
		ClassNameCamelCase = $"{char.ToLowerInvariant(ClassName[0])}{ClassName.AsSpan(1).ToString()}";
	}
}