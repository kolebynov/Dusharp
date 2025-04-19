namespace Dusharp.SourceGenerator.Common.CodeAnalyzing;

public readonly struct UnionCaseParameterInfo
{
	public string Name { get; }

	public TypeName TypeName { get; }

	public bool ContainsGenericParameters { get; }

	public UnionCaseParameterInfo(string name, TypeName typeName, bool containsGenericParameters)
	{
		Name = name;
		TypeName = typeName;
		ContainsGenericParameters = containsGenericParameters;
	}
}