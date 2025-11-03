namespace Dusharp;

public sealed class UnionDescription
{
	public string Name { get; }

	public IReadOnlyList<UnionCaseDescription> Cases { get; }

	public UnionDescription(string name, params UnionCaseDescription[] cases)
	{
		Name = name;
		Cases = cases;
	}
}