namespace Dusharp.SourceGenerator.CodeAnalyzing;

public sealed class UnionInfo
{
	public string Name { get; }

	public IReadOnlyList<UnionCaseInfo> Cases { get; }

	public TypeInfo TypeInfo { get; }

	public UnionInfo(string name, IReadOnlyList<UnionCaseInfo> cases, TypeInfo typeInfo)
	{
		Name = name;
		Cases = cases;
		TypeInfo = typeInfo;
	}
}