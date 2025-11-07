namespace Dusharp;

public interface IUnion
{
	string CaseName { get; }

	void GetCaseParameters(Span<object?> parameters);
}

public interface IUnion<TSelf> : IUnion
	where TSelf : IUnion<TSelf>
{
#if NET8_0_OR_GREATER
	static abstract UnionDescription UnionDescription { get; }

	static abstract TSelf CreateUnion(string name, ReadOnlySpan<object?> parameters);
#endif
}