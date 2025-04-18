namespace Dusharp.SourceGenerator.CodeAnalyzing;

public readonly struct TypeName : IEquatable<TypeName>
{
	private readonly bool _isRefNullable;

	public TypeInfo TypeInfo { get; }

	public string Name => TypeInfo.Name;

	public string FullyQualifiedName => TypeInfo.GetFullyQualifiedName(_isRefNullable);

	public TypeName(TypeInfo typeInfo, bool isRefNullable)
	{
		TypeInfo = typeInfo;
		_isRefNullable = isRefNullable;
	}

	public bool Equals(TypeName other) => _isRefNullable == other._isRefNullable && TypeInfo.Equals(other.TypeInfo);

	public override bool Equals(object? obj) => obj is TypeName other && Equals(other);

	public override int GetHashCode()
	{
		unchecked
		{
			return (_isRefNullable.GetHashCode() * 397) ^ TypeInfo.GetHashCode();
		}
	}

	public override string ToString() => FullyQualifiedName;

	public static bool operator ==(TypeName left, TypeName right) => left.Equals(right);

	public static bool operator !=(TypeName left, TypeName right) => !(left == right);
}