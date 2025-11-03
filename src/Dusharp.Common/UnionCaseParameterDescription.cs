namespace Dusharp;

public readonly struct UnionCaseParameterDescription
{
	public string Name { get; }

	public Type Type { get; }

	public UnionCaseParameterDescription(string name, Type type)
	{
		Name = name;
		Type = type;
	}
}