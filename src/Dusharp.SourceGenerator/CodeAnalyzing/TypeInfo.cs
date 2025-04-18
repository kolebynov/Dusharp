namespace Dusharp.SourceGenerator.CodeAnalyzing;

public sealed partial class TypeInfo : IEquatable<TypeInfo>
{
	private readonly bool _isReferenceType;
	private readonly string _fullyQualifiedName;

	public string Name { get; }

	public string? Namespace { get; }

	public TypeInfo? ContainingType { get; }

	public TypeKind Kind { get; }

	public TypeInfo(string? ns, TypeInfo? containingType, string name, string fullyQualifiedName, TypeKind kind)
	{
		Name = name;
		Namespace = ns;
		ContainingType = containingType;
		Kind = kind;
		_fullyQualifiedName = fullyQualifiedName;
		_isReferenceType = kind.Match(_ => true, _ => false, () => true);
	}

	public string GetFullyQualifiedName(bool isRefNullable) =>
		_isReferenceType && isRefNullable ? $"{_fullyQualifiedName}?" : _fullyQualifiedName;

	public bool Equals(TypeInfo? other)
	{
		if (other is null)
		{
			return false;
		}

		if (ReferenceEquals(this, other))
		{
			return true;
		}

		return _fullyQualifiedName == other._fullyQualifiedName;
	}

	public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is TypeInfo other && Equals(other));

	public override int GetHashCode() => _fullyQualifiedName.GetHashCode();

	public override string ToString() => GetFullyQualifiedName(false);

	public static TypeInfo SpecificType(string? ns, TypeInfo? containingType, string name, TypeKind kind)
	{
		var namespacePart = !string.IsNullOrEmpty(ns) ? $"{ns}." : string.Empty;
		var containingTypesPart = containingType != null
			? $"{string.Join(".", FlattenNesting(containingType))}."
			: string.Empty;
		return new TypeInfo(ns, containingType, name, $"global::{namespacePart}{containingTypesPart}{name}", kind);
	}

	public static TypeInfo SpecialName(string name, TypeKind kind) => new(null, null, name, name, kind);

	public static bool operator ==(TypeInfo? left, TypeInfo? right) => Equals(left, right);

	public static bool operator !=(TypeInfo? left, TypeInfo? right) => !Equals(left, right);

	private static IEnumerable<string> FlattenNesting(TypeInfo? typeInfo)
	{
		if (typeInfo is null)
		{
			yield break;
		}

		foreach (var name in FlattenNesting(typeInfo.ContainingType))
		{
			yield return name;
		}

		yield return typeInfo.Name;
	}

	[Union]
	public partial class TypeKind
	{
		[UnionCase]
		public static partial TypeKind ReferenceType(bool isInterface);

		[UnionCase]
		public static partial TypeKind ValueType(bool isUnmanaged);

		[UnionCase]
		public static partial TypeKind Unknown();

		public string ToCodeString() =>
			Match(
				isInterface => isInterface ? "interface" : "class",
				_ => "struct",
				() => throw new InvalidOperationException("Unknown type"));
	}
}