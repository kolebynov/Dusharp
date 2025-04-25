using System.Text;

namespace Dusharp.SourceGenerator.Common.CodeAnalyzing;

public static class TypeInfos
{
	public const string SystemNs = "System";
	public const string CompilerServicesNs = $"{SystemNs}.Runtime.CompilerServices";
	public const string CollectionsGenericNs = $"{SystemNs}.Collections.Generic";
	public const string DusharpNs = "Dusharp";

	private static readonly TypeInfo ParameterlessAction =
		TypeInfo.SpecificType(SystemNs, null, "Action", TypeInfo.TypeKind.ReferenceType(false));

	public static readonly TypeInfo Void = TypeInfo.SpecialName("void", TypeInfo.TypeKind.Unknown());
	public static readonly TypeInfo Object = TypeInfo.SpecificType(SystemNs, null, "Object", TypeInfo.TypeKind.ReferenceType(false));
	public static readonly TypeInfo String = TypeInfo.SpecificType(SystemNs, null, "String", TypeInfo.TypeKind.ReferenceType(false));
	public static readonly TypeInfo Boolean = TypeInfo.SpecificType(SystemNs, null, "Boolean", TypeInfo.TypeKind.ValueType(true));
	public static readonly TypeInfo Int32 = TypeInfo.SpecificType(SystemNs, null, "Int32", TypeInfo.TypeKind.ValueType(true));
	public static readonly TypeInfo Byte = TypeInfo.SpecificType(SystemNs, null, "Byte", TypeInfo.TypeKind.ValueType(true));
	public static readonly TypeInfo Type = TypeInfo.SpecificType(SystemNs, null, "Type", TypeInfo.TypeKind.ReferenceType(false));
	public static readonly TypeInfo Unsafe = TypeInfo.SpecificType(CompilerServicesNs, null, "Unsafe", TypeInfo.TypeKind.ReferenceType(false));

	public static readonly TypeInfo IUnion = TypeInfo.SpecificType(DusharpNs, null, "IUnion", TypeInfo.TypeKind.ReferenceType(true));

	public static TypeInfo IEquatable(TypeName arg) =>
		TypeInfo.SpecificType(SystemNs, null, $"IEquatable<{arg.FullyQualifiedName}>", TypeInfo.TypeKind.ReferenceType(true));

	public static TypeInfo Action(IReadOnlyCollection<TypeName> args) =>
		args.Count > 0
			? TypeInfo.SpecificType(SystemNs, null, $"Action<{string.Join(", ", args.Select(x => x.FullyQualifiedName))}>", TypeInfo.TypeKind.ReferenceType(false))
			: ParameterlessAction;

	public static TypeInfo Func(IReadOnlyCollection<TypeName> parameterArgs, TypeName returnArg)
	{
		var parametersString = parameterArgs
			.Aggregate(new StringBuilder(), (sb, t) => sb.Append($"{t.FullyQualifiedName}, "), sb => sb.ToString());
		return TypeInfo.SpecificType(SystemNs, null, $"Func<{parametersString}{returnArg.FullyQualifiedName}>",
			TypeInfo.TypeKind.ReferenceType(false));
	}

	public static TypeInfo EqualityComparer(TypeName arg) =>
		TypeInfo.SpecificType(CollectionsGenericNs, null, $"EqualityComparer<{arg.FullyQualifiedName}>", TypeInfo.TypeKind.ReferenceType(false));
}