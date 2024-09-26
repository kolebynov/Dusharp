using System;
using Dusharp.CodeAnalyzing;
using Dusharp.CodeGeneration;

namespace Dusharp.UnionGeneration;

public interface IUnionDefinitionGenerator
{
	TypeKind TypeKind { get; }

	Action<MethodDefinition, CodeWriter> GetUnionCaseMethodBodyWriter(UnionCaseInfo unionCase);

	MethodDefinition AdjustDefaultEqualsMethod(MethodDefinition equalsMethod);

	MethodDefinition AdjustSpecificEqualsMethod(MethodDefinition equalsMethod);

	Action<MethodDefinition, CodeWriter> GetGetHashCodeMethodBodyWriter();

	Action<OperatorDefinition, CodeWriter> GetEqualityOperatorBodyWriter();

	void WriteMatchBlock(UnionCaseInfo unionCase, Func<string, string> matchedCaseDelegateCallProvider,
		CodeWriter matchBlock);

	TypeDefinition AddAdditionalInfo(TypeDefinition typeDefinition);
}