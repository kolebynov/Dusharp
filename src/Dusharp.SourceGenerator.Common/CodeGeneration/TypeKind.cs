namespace Dusharp.SourceGenerator.Common.CodeGeneration;

public abstract record TypeKind
{
	private TypeKind()
	{
	}

	public sealed record Class(bool IsAbstract, bool IsSealed) : TypeKind;

	public sealed record Struct(bool IsReadOnly) : TypeKind;
}