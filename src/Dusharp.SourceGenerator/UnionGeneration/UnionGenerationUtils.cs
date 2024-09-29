using System.Collections.Generic;
using System.Linq;

namespace Dusharp.UnionGeneration;

public static class UnionGenerationUtils
{
	public static readonly string ThrowUnionInInvalidStateCode =
		$"{typeof(ExceptionUtils).FullName}.{nameof(ExceptionUtils.ThrowUnionInInvalidState)}();";

	public static string GetCaseStringRepresentation(
		string unionCaseName, IReadOnlyCollection<(string ParameterName, string ParameterValue)> parameters) =>
		parameters.Count == 0
			? $"\"{unionCaseName}\""
			: $"$\"{unionCaseName} {{{{ {string.Join(", ", parameters.Select(x => $"{x.ParameterName} = {{{x.ParameterValue}}}"))} }}}}\"";
}