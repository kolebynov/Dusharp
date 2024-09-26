using System.Collections.Generic;

namespace Dusharp.CodeGeneration;

public readonly struct TypeName
{
	public string Name { get; }

	public string FullName { get; }

	public TypeName(string name, IReadOnlyList<string> genericParameters)
	{
		Name = name;
		var genericParametersStr = genericParameters.Count > 0
			? $"<{string.Join(", ", genericParameters)}>"
			: string.Empty;
		FullName = $"{Name}{genericParametersStr}";
	}
}