namespace Dusharp.CodeAnalyzing;

public readonly struct UnionCaseParameterInfo
{
	public string Name { get; }

	public string TypeName { get; }

	public UnionCaseParameterInfo(string name, string typeName)
	{
		Name = name;
		TypeName = typeName;
	}
}