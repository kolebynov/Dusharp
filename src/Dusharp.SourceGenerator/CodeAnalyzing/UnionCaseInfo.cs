using System.Collections.Generic;

namespace Dusharp.CodeAnalyzing;

public sealed class UnionCaseInfo
{
	public string Name { get; }

	public IReadOnlyList<UnionCaseParameterInfo> Parameters { get; }

	public bool HasParameters => Parameters.Count > 0;

	public UnionCaseInfo(string name, IReadOnlyList<UnionCaseParameterInfo> parameters)
	{
		Name = name;
		Parameters = parameters;
	}
}