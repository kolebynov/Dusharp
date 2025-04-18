using Dusharp.SourceGenerator.CodeAnalyzing;
using Dusharp.SourceGenerator.CodeGeneration;

namespace Dusharp.SourceGenerator.UnionGeneration;

public interface IUnionDefinitionGenerator
{
	TypeKind TypeKind { get; }

	Action<MethodDefinition, CodeWriter> GetUnionCaseMethodBodyWriter(UnionCaseInfo unionCase);

	string GetUnionCaseCheckExpression(UnionCaseInfo unionCase);

	IEnumerable<string> GetUnionCaseParameterAccessors(UnionCaseInfo unionCase);

	MethodDefinition AdjustDefaultEqualsMethod(MethodDefinition equalsMethod);

	MethodDefinition AdjustSpecificEqualsMethod(MethodDefinition equalsMethod);

	Action<MethodDefinition, CodeWriter> GetGetHashCodeMethodBodyWriter();

	Action<OperatorDefinition, CodeWriter> GetEqualityOperatorBodyWriter();

	TypeDefinition AdjustUnionTypeDefinition(TypeDefinition typeDefinition);

	IReadOnlyList<TypeDefinition> GetAdditionalTypes();
}