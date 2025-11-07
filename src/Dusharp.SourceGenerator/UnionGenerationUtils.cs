using System.Globalization;
using Dusharp.SourceGenerator.Common.CodeAnalyzing;

namespace Dusharp.SourceGenerator;

public static class UnionGenerationUtils
{
	public static readonly string ThrowUnionInInvalidStateCode =
		$"{typeof(ExceptionUtils).FullName}.{nameof(ExceptionUtils.ThrowUnionInInvalidState)}();";

	public static string GetCaseStringRepresentation(
		string unionCaseName, IReadOnlyCollection<(string ParameterName, string ParameterValue)> parameters) =>
		parameters.Count == 0
			? $"\"{unionCaseName}\""
			: $"$\"{unionCaseName} {{{{ {string.Join(", ", parameters.Select(x => $"{x.ParameterName} = {{({x.ParameterValue})}}"))} }}}}\"";

	public static string GetUnionCaseEqualityCode(IEnumerable<(TypeName Type, string Left, string Right)> parameters)
	{
		var conditions = parameters
			.Select(x =>
				$"{TypeInfos.EqualityComparer(x.Type)}.Default.Equals({x.Left}, {x.Right})");
		return string.Join(" && ", conditions);
	}

	public static string GetUnionCaseHashCodeCode(int caseIndex, IEnumerable<(TypeName Type, string Value)> parameters)
	{
		var hashCodes = parameters
			.Select(x =>
				$"{TypeInfos.EqualityComparer(x.Type)}.Default.GetHashCode({x.Value}!)")
			.Prepend(caseIndex.ToString(CultureInfo.InvariantCulture));

		return $"unchecked {{ return {string.Join(" * -1521134295 + ", hashCodes)}; }}";
	}

	public static string ThrowInvalidParametersCount(UnionCaseInfo unionCase, string parametersName) =>
		$"{typeof(ExceptionUtils).FullName}.{nameof(ExceptionUtils.ThrowInvalidParametersCount)}(\"{unionCase.Name}\", {unionCase.Parameters.Count}, {parametersName}.Length, nameof({parametersName}));";

	public static string ThrowCaseDoesNotExist(string unionName, string caseNameParameter) =>
		$"{typeof(ExceptionUtils).FullName}.{nameof(ExceptionUtils.ThrowCaseDoesNotExist)}({caseNameParameter}, \"{unionName}\", nameof({caseNameParameter}));";
}