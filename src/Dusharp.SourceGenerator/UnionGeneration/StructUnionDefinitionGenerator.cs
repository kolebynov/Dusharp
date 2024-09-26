using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Dusharp.CodeAnalyzing;
using Dusharp.CodeGeneration;
using Dusharp.Extensions;
using Microsoft.CodeAnalysis;
using TypeKind = Dusharp.CodeGeneration.TypeKind;

namespace Dusharp.UnionGeneration;

public sealed class StructUnionDefinitionGenerator : IUnionDefinitionGenerator
{
	private readonly UnionInfo _union;
	private readonly TypeName _unionTypeName;
	private readonly UnionImplementationGenerator _unionImplementationGenerator;
	private readonly IReadOnlyDictionary<UnionCaseInfo, IReadOnlyList<CaseParameter>> _casesParameters;

	public TypeKind TypeKind => TypeKind.Struct(false);

	public StructUnionDefinitionGenerator(UnionInfo union)
	{
		_union = union;
		_unionTypeName = union.GetTypeName();
		_unionImplementationGenerator = new UnionImplementationGenerator();
		_casesParameters = union.Cases
			.ToDictionary(
				unionCase => unionCase,
				IReadOnlyList<CaseParameter> (unionCase) => unionCase.Parameters
					.Select(caseParameter => _unionImplementationGenerator.AddCaseParameter(caseParameter, unionCase))
					.ToArray());
	}

	public Action<MethodDefinition, CodeWriter> GetUnionCaseMethodBodyWriter(UnionCaseInfo unionCase)
	{
		var unionCaseIndex = GetUnionCaseIndex(unionCase);
		var caseParameters = _casesParameters[unionCase];

		return (def, methodBlock) =>
		{
			methodBlock
				.Append("var result = new ")
				.Append(_unionTypeName.FullName)
				.Append(" { Index = ")
				.Append(unionCaseIndex.ToString(CultureInfo.InvariantCulture))
				.AppendLine(" };");

			foreach (var (methodParameter, caseParameter) in def.Parameters.Zip(caseParameters, (x, y) => (x, y)))
			{
				methodBlock
					.Append("result.")
					.Append(caseParameter.FieldPath)
					.Append(" = ")
					.Append(methodParameter.Name)
					.AppendLine(";");
			}

			methodBlock.AppendLine("return result;");
		};
	}

	public MethodDefinition AdjustDefaultEqualsMethod(MethodDefinition equalsMethod) =>
		equalsMethod with
		{
			BodyWriter = (def, methodBlock) => methodBlock
				.Append("return ")
				.Append(def.Parameters[0].Name)
				.Append(" is ")
				.Append(_unionTypeName.FullName)
				.Append(" && Equals((")
				.Append(_unionTypeName.FullName)
				.Append(")")
				.Append(def.Parameters[0].Name)
				.AppendLine(");"),
		};

	public MethodDefinition AdjustSpecificEqualsMethod(MethodDefinition equalsMethod) =>
		equalsMethod with
		{
			BodyWriter = (def, methodBlock) => methodBlock.AppendLine("return false;"),
		};

	public Action<MethodDefinition, CodeWriter> GetGetHashCodeMethodBodyWriter()
	{
		return (def, methodBlock) => methodBlock.AppendLine("return 0;");
	}

	public Action<OperatorDefinition, CodeWriter> GetEqualityOperatorBodyWriter() =>
		(def, operatorBlock) => operatorBlock
			.Append("return ")
			.Append(def.Parameters[0].Name)
			.Append(".Equals(")
			.Append(def.Parameters[1].Name)
			.AppendLine(");");

	public void WriteMatchBlock(UnionCaseInfo unionCase, Func<string, string> matchedCaseDelegateCallProvider,
		CodeWriter matchBlock)
	{
		var unionCaseIndex = GetUnionCaseIndex(unionCase);
		var caseParameters = _casesParameters[unionCase];
		var argumentsStr = string.Join(", ", caseParameters.Select(x => x.ValueAccessor));
		matchBlock
			.Append("if (Index == ")
			.Append(unionCaseIndex.ToString(CultureInfo.InvariantCulture))
			.Append(") { ")
			.Append(matchedCaseDelegateCallProvider(argumentsStr))
			.AppendLine(" }");
	}

	public TypeDefinition AddAdditionalInfo(TypeDefinition typeDefinition)
	{
		typeDefinition = _unionImplementationGenerator.AdjustUnionTypeDefinition(typeDefinition);
		return typeDefinition with
		{
			Attributes =
			[
				.. typeDefinition.Attributes,
				GetLayoutAttribute(LayoutKind.Auto),
			],
			Fields =
			[
				.. typeDefinition.Fields,
				new FieldDefinition
				{
					Accessibility = Accessibility.Private,
					TypeName = "byte",
					Name = "Index",
				},
			],
		};
	}

	private int GetUnionCaseIndex(UnionCaseInfo unionCase) => _union.Cases.IndexOf(unionCase) + 1;

	private static string GetLayoutAttribute(LayoutKind layoutKind) =>
		$"System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.{layoutKind})";

	private readonly record struct CaseParameter(string FieldPath, string ValueAccessor);

	private sealed class UnionImplementationGenerator
	{
		private const string BlittableStructName = "UnionBlittableData";
		private const string BlittableStructFieldName = "UnionBlittableDataField";

		private readonly List<DefaultParameterInfo> _defaultParameters = [];
		private readonly Dictionary<UnionCaseInfo, CaseBlittableStructInfo> _blittableParameters = new();

		public CaseParameter AddCaseParameter(UnionCaseParameterInfo caseParameter, UnionCaseInfo unionCase)
		{
			switch (caseParameter.Type)
			{
				case { IsUnmanagedType: true }:
					if (!_blittableParameters.TryGetValue(unionCase, out var blittableStructInfo))
					{
						blittableStructInfo = new CaseBlittableStructInfo(
							$"{unionCase.Name}BlittableData", $"{unionCase.Name}Data", []);
						_blittableParameters[unionCase] = blittableStructInfo;
					}

					blittableStructInfo.Fields.Add(new TypeNamePair(caseParameter.TypeName, caseParameter.Name));
					var fieldPath = $"{BlittableStructFieldName}.{blittableStructInfo.FieldName}.{caseParameter.Name}";
					return new CaseParameter(fieldPath, $"this.{fieldPath}");

				case { IsReferenceType: true }:
					var referenceTypeParameter = GetOrAddDefaultParameter("object", unionCase);
					return new CaseParameter(
						referenceTypeParameter.Name,
						$"System.Runtime.CompilerServices.Unsafe.As<{caseParameter.TypeName}>(this.{referenceTypeParameter.Name})");

				default:
					var defaultParameter = GetOrAddDefaultParameter(caseParameter.TypeName, unionCase);
					return new CaseParameter(defaultParameter.Name, $"this.{defaultParameter.Name}");
			}
		}

		public TypeDefinition AdjustUnionTypeDefinition(TypeDefinition typeDefinition)
		{
			var blittableDataField = _blittableParameters.Count > 0
				? new FieldDefinition
				{
					Accessibility = Accessibility.Private,
					TypeName = BlittableStructName,
					Name = BlittableStructFieldName,
				}
				: null;
			var blittableDataNestedType = GenerateBlittableDataNestedType();

			return typeDefinition with
			{
				Fields =
				[
					.. typeDefinition.Fields,
					.. _defaultParameters
						.Select(p => new FieldDefinition
						{
							Accessibility = Accessibility.Private,
							TypeName = p.Field.Type,
							Name = p.Field.Name,
						}),
					.. blittableDataField != null ? [blittableDataField] : Array.Empty<FieldDefinition>(),
				],
				NestedTypes =
				[
					.. typeDefinition.NestedTypes,
					.. blittableDataNestedType != null ? [blittableDataNestedType] : Array.Empty<TypeDefinition>(),
				],
			};
		}

		private TypeNamePair GetOrAddDefaultParameter(string typeName, UnionCaseInfo unionCase)
		{
			var defaultParameter = _defaultParameters.Find(x => x.Field.Type == typeName && !x.UsedBy.Contains(unionCase));
			if (defaultParameter.UsedBy == null)
			{
				defaultParameter = new DefaultParameterInfo(
					new TypeNamePair(typeName, $"Field{_defaultParameters.Count}"), []);
				_defaultParameters.Add(defaultParameter);
			}

			defaultParameter.UsedBy.Add(unionCase);
			return defaultParameter.Field;
		}

		private TypeDefinition? GenerateBlittableDataNestedType()
		{
			if (_blittableParameters.Count == 0)
			{
				return null;
			}

			return new TypeDefinition
			{
				Accessibility = Accessibility.Private,
				Kind = TypeKind.Struct(false),
				Name = UnionImplementationGenerator.BlittableStructName,
				Attributes = [GetLayoutAttribute(LayoutKind.Explicit)],
				Fields = _blittableParameters.Values
					.Select(x => new FieldDefinition
					{
						Accessibility = Accessibility.Public,
						TypeName = x.StructName,
						Name = x.FieldName,
						Attributes = ["System.Runtime.InteropServices.FieldOffsetAttribute(0)"],
					})
					.ToArray(),
				NestedTypes = _blittableParameters.Values
					.Select(x => new TypeDefinition
					{
						Accessibility = Accessibility.Public,
						Kind = TypeKind.Struct(false),
						Name = x.StructName,
						Attributes = [GetLayoutAttribute(LayoutKind.Auto)],
						Fields = x.Fields
							.Select(y => new FieldDefinition
							{
								Accessibility = Accessibility.Public,
								TypeName = y.Type,
								Name = y.Name,
							})
							.ToArray(),
					})
					.ToArray(),
			};
		}

		private readonly record struct DefaultParameterInfo(TypeNamePair Field, HashSet<UnionCaseInfo> UsedBy);

		private readonly record struct TypeNamePair(string Type, string Name);

		private readonly record struct CaseBlittableStructInfo(string StructName, string FieldName, List<TypeNamePair> Fields);
	}
}