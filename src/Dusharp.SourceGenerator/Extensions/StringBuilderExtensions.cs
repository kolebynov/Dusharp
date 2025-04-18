using System.Text;

namespace Dusharp.SourceGenerator.Extensions;

public static class StringBuilderExtensions
{
	public static bool EndsWith(this StringBuilder stringBuilder, string value)
	{
		if (stringBuilder.Length < value.Length)
		{
			return false;
		}

		var sbIndex = stringBuilder.Length - value.Length;
		foreach (var ch in value)
		{
			if (stringBuilder[sbIndex] != ch)
			{
				return false;
			}

			sbIndex++;
		}

		return true;
	}
}