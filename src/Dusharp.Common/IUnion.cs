namespace Dusharp;

#pragma warning disable CA1040
public interface IUnion
{
}

public interface IUnion<TSelf>
	where TSelf : IUnion<TSelf>
{
#if NET8_0_OR_GREATER
	static abstract UnionDescription UnionDescription { get; }

	static abstract TSelf Create(string name, ReadOnlySpan<object> parameters);
#endif
}