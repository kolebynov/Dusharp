using System.Text;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

namespace Dusharp.SourceGenerator.Tests;

public class UnionGenerationTests
{
	[Fact]
	public async Task ForStructUnion_GenerateCorrectCode()
	{
		await TestSourceGenerator("StructUnion", "TestUnion.StructUnion(T1, T2, T3)");
	}

	[Fact]
	public async Task ForClassUnion_GenerateCorrectCode()
	{
		await TestSourceGenerator("ClassUnion", "TestUnion.ClassUnion(T1, T2, T3)");
	}

	private static Task TestSourceGenerator(string fileName, string typeFullName)
	{
		var test = new CSharpSourceGeneratorTest<UnionSourceGenerator, DefaultVerifier>
		{
			TestState =
			{
				Sources = { File.ReadAllText(Path.Combine("TestData", $"{fileName}.cs")) },
				AdditionalReferences = { typeof(UnionAttribute).Assembly },
				GeneratedSources =
				{
					(Path.Combine("Dusharp.SourceGenerator", "Dusharp.SourceGenerator.UnionSourceGenerator", $"{typeFullName}.Dusharp.Union.g.cs"), SourceText.From(File.ReadAllText(Path.Combine("TestData", $"{fileName}.Generated.cs")), Encoding.UTF8)),
				},
			},
		};

		return test.RunAsync();
	}
}