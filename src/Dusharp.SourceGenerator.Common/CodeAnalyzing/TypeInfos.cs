using System.Text;

namespace Dusharp.SourceGenerator.Common.CodeAnalyzing;

public static class TypeInfos
{
	public const string SystemNs = "System";
	public const string CompilerServicesNs = $"{SystemNs}.Runtime.CompilerServices";
	public const string CollectionsGenericNs = $"{SystemNs}.Collections.Generic";
	public const string DusharpNs = "Dusharp";

	private static readonly TypeInfo ParameterlessAction =
		TypeInfo.SpecificType(SystemNs, null, "Action", new TypeInfo.TypeKind.ReferenceType(false));

	public static readonly TypeInfo Void = TypeInfo.SpecialName("void", new TypeInfo.TypeKind.Unknown());
	public static readonly TypeInfo Object = TypeInfo.SpecificType(SystemNs, null, "Object", new TypeInfo.TypeKind.ReferenceType(false));
	public static readonly TypeInfo String = TypeInfo.SpecificType(SystemNs, null, "String", new TypeInfo.TypeKind.ReferenceType(false));
	public static readonly TypeInfo Boolean = TypeInfo.SpecificType(SystemNs, null, "Boolean", new TypeInfo.TypeKind.ValueType(true));
	public static readonly TypeInfo Int32 = TypeInfo.SpecificType(SystemNs, null, "Int32", new TypeInfo.TypeKind.ValueType(true));
	public static readonly TypeInfo Byte = TypeInfo.SpecificType(SystemNs, null, "Byte", new TypeInfo.TypeKind.ValueType(true));
	public static readonly TypeInfo Type = TypeInfo.SpecificType(SystemNs, null, "Type", new TypeInfo.TypeKind.ReferenceType(false));
	public static readonly TypeInfo Unsafe = TypeInfo.SpecificType(CompilerServicesNs, null, "Unsafe", new TypeInfo.TypeKind.ReferenceType(false));

	public static readonly TypeInfo UnionDescription = TypeInfo.SpecificType(DusharpNs, null, "UnionDescription", new TypeInfo.TypeKind.ReferenceType(false));
	public static readonly TypeInfo UnionCaseDescription = TypeInfo.SpecificType(DusharpNs, null, "UnionCaseDescription", new TypeInfo.TypeKind.ReferenceType(false));
	public static readonly TypeInfo UnionUnionCaseParameterDescription = TypeInfo.SpecificType(DusharpNs, null, "UnionCaseParameterDescription", new TypeInfo.TypeKind.ReferenceType(false));

	public static TypeInfo IEquatable(TypeName arg) =>
		TypeInfo.SpecificType(SystemNs, null, $"IEquatable<{arg.FullyQualifiedName}>", new TypeInfo.TypeKind.ReferenceType(true));

	public static TypeInfo Action(IReadOnlyCollection<TypeName> args) =>
		args.Count > 0
			? TypeInfo.SpecificType(
				SystemNs, null, $"Action<{string.Join(", ", args.Select(x => x.FullyQualifiedName))}>",
				new TypeInfo.TypeKind.ReferenceType(false))
			: ParameterlessAction;

	public static TypeInfo Func(IReadOnlyCollection<TypeName> parameterArgs, TypeName returnArg)
	{
		var parametersString = parameterArgs
#pragma warning disable CA1305
			.Aggregate(new StringBuilder(), (sb, t) => sb.Append($"{t.FullyQualifiedName}, "), sb => sb.ToString());
#pragma warning restore CA1305
		return TypeInfo.SpecificType(SystemNs, null, $"Func<{parametersString}{returnArg.FullyQualifiedName}>",
			new TypeInfo.TypeKind.ReferenceType(false));
	}

	public static TypeInfo EqualityComparer(TypeName arg) =>
		TypeInfo.SpecificType(CollectionsGenericNs, null, $"EqualityComparer<{arg.FullyQualifiedName}>", new TypeInfo.TypeKind.ReferenceType(false));

	public static TypeInfo ValueTuple(params TypeName[] tupleTypes) =>
		TypeInfo.SpecificType(SystemNs, null,
			$"ValueTuple<{string.Join(", ", tupleTypes.Select(x => x.FullyQualifiedName))}>",
			new TypeInfo.TypeKind.ValueType(tupleTypes.All(x => x.TypeInfo.Kind is TypeInfo.TypeKind.ValueType { IsUnmanaged: true })));

	public static TypeInfo Span(TypeName itemType) =>
		TypeInfo.SpecificType(SystemNs, null, $"Span<{itemType}>", new TypeInfo.TypeKind.ValueType(true));

	public static TypeInfo ReadOnlySpan(TypeName itemType) =>
		TypeInfo.SpecificType(SystemNs, null, $"ReadOnlySpan<{itemType}>", new TypeInfo.TypeKind.ValueType(true));

	public static TypeInfo IUnion(TypeInfo unionType) =>
		TypeInfo.SpecificType(DusharpNs, null, $"IUnion<{unionType}>", new TypeInfo.TypeKind.ReferenceType(true));
}