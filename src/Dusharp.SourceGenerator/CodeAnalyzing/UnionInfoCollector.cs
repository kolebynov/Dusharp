using Microsoft.CodeAnalysis;

namespace Dusharp.CodeAnalyzing;

public static class UnionInfoCollector
{
	public static UnionInfo Collect(INamedTypeSymbol unionTypeSymbol, INamedTypeSymbol unionCaseAttributeSymbol)
	{
		var unionCases = unionTypeSymbol.GetMembers()
			.OfType<IMethodSymbol>()
			.Where(methodSymbol => methodSymbol.GetAttributes()
				.Any(attr =>
					attr.AttributeClass?.Equals(unionCaseAttributeSymbol, SymbolEqualityComparer.Default) ??
					false))
			.Select(x => new UnionCaseInfo(
				x.Name,
				x.Parameters
					.Select(y => new UnionCaseParameterInfo(
						y.Name,
						new TypeName(CreateTypeInfo(y.Type), y.NullableAnnotation == NullableAnnotation.Annotated),
						ContainsGenericParameters(y.Type)))
					.ToArray()))
			.ToArray();

		return new UnionInfo(unionTypeSymbol.Name, unionCases, CreateTypeInfo(unionTypeSymbol));
	}

	private static TypeInfo CreateTypeInfo(ITypeSymbol typeSymbol) =>
		new(
			typeSymbol.ContainingNamespace is { IsGlobalNamespace: false }
				? typeSymbol.ContainingNamespace.ToDisplayString()
				: null,
			typeSymbol.ContainingType != null ? CreateTypeInfo(typeSymbol.ContainingType) : null,
			typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
			typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			typeSymbol switch
			{
				{ IsUnmanagedType: true } => TypeInfo.TypeKind.ValueType(true),
				{ IsValueType: true } => TypeInfo.TypeKind.ValueType(false),
				{ IsReferenceType: true } => TypeInfo.TypeKind.ReferenceType(typeSymbol.TypeKind == TypeKind.Interface),
				_ => TypeInfo.TypeKind.Unknown(),
			});

	private static bool ContainsGenericParameters(ITypeSymbol typeSymbol) =>
		typeSymbol switch
		{
			ITypeParameterSymbol => true,
			INamedTypeSymbol { IsGenericType: true } namedTypeSymbol => namedTypeSymbol.TypeArguments.Any(ContainsGenericParameters),
			IArrayTypeSymbol arrayTypeSymbol => ContainsGenericParameters(arrayTypeSymbol.ElementType),
			_ => false,
		};
}