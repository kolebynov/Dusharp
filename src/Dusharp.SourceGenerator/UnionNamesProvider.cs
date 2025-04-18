namespace Dusharp.SourceGenerator;

internal static class UnionNamesProvider
{
	public static string GetIsCasePropertyName(string unionCaseName) => $"Is{unionCaseName}";

	public static string GetTryGetCaseDataMethodName(string unionCaseName) => $"TryGet{unionCaseName}Data";
}