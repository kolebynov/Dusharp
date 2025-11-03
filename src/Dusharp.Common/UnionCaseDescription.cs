namespace Dusharp;

public sealed class UnionCaseDescription
{
	public string Name { get; }

	public IReadOnlyList<UnionCaseParameterDescription> Parameters { get; }

	public UnionCaseDescription(string name, params UnionCaseParameterDescription[] parameters)
	{
		Name = name;
		Parameters = parameters;
	}
}