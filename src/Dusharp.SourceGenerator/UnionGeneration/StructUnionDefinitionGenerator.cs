using System.Globalization;
using System.Runtime.InteropServices;
using Dusharp.CodeAnalyzing;
using Dusharp.CodeGeneration;
using Dusharp.Extensions;
using Microsoft.CodeAnalysis;
using TypeInfo = Dusharp.CodeAnalyzing.TypeInfo;
using TypeKind = Dusharp.CodeGeneration.TypeKind;
using TypeName = Dusharp.CodeAnalyzing.TypeName;

namespace Dusharp.UnionGeneration;

public sealed class StructUnionDefinitionGenerator : IUnionDefinitionGenerator
{
	private readonly UnionInfo _union;
	private readonly UnionImplementationGenerator _unionImplementationGenerator;
	private readonly IReadOnlyDictionary<UnionCaseInfo, IReadOnlyList<CaseParameter>> _casesParameters;

	public TypeKind TypeKind => TypeKind.Struct(false);

	public StructUnionDefinitionGenerator(UnionInfo union)
	{
		_union = union;
		_unionImplementationGenerator = new UnionImplementationGenerator(
			TypeInfo.SpecificType(union.TypeInfo.Namespace, union.TypeInfo.ContainingType, $"{union.Name}BlittableData",
				TypeInfo.TypeKind.ValueType(true)));
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
				.Append(_union.TypeInfo.GetFullyQualifiedName(false))
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
					.AppendLine("!;");
			}

			methodBlock.AppendLine("return result;");
		};
	}

	public string GetUnionCaseCheckExpression(UnionCaseInfo unionCase)
	{
		var unionCaseIndex = GetUnionCaseIndex(unionCase);
		return $"Index == {unionCaseIndex}";
	}

	public IEnumerable<string> GetUnionCaseParameterAccessors(UnionCaseInfo unionCase)
	{
		var caseParameters = _casesParameters[unionCase];
		return caseParameters.Select(x => x.ValueAccessor("this"));
	}

	public MethodDefinition AdjustDefaultEqualsMethod(MethodDefinition equalsMethod) =>
		equalsMethod with
		{
			BodyWriter = (def, methodBlock) => methodBlock
				.Append("return ")
				.Append(def.Parameters[0].Name)
				.Append(" is ")
				.Append(_union.TypeInfo)
				.Append(" && Equals((")
				.Append(_union.TypeInfo)
				.Append(")")
				.Append(def.Parameters[0].Name)
				.AppendLine(");"),
		};

	public MethodDefinition AdjustSpecificEqualsMethod(MethodDefinition equalsMethod) =>
		equalsMethod with
		{
			BodyWriter = (def, methodBlock) =>
			{
				var otherName = def.Parameters[0].Name;

				methodBlock
					.Append("if (this.Index != ")
					.Append(otherName)
					.AppendLine(".Index) return false;");

				WriteCasesSwitchBody(
					methodBlock,
					(unionCase, caseParameters) =>
					{
						var equalityCode = unionCase.HasParameters
							? UnionGenerationUtils.GetUnionCaseEqualityCode(
								caseParameters.Select(x => (Type: x.TypeName, x.ValueAccessor("this"), x.ValueAccessor(otherName))))
							: "true";
						return $"return {equalityCode};";
					});

				methodBlock.AppendLine("return true;");
			},
		};

	public Action<MethodDefinition, CodeWriter> GetGetHashCodeMethodBodyWriter() =>
		(_, methodBlock) =>
		{
			WriteCasesSwitchBody(
				methodBlock,
				(unionCase, caseParameters) => UnionGenerationUtils.GetUnionCaseHashCodeCode(
					GetUnionCaseIndex(unionCase), caseParameters.Select(x => (Type: x.TypeName, x.ValueAccessor("this")))));
			methodBlock.AppendLine("return 0;");
		};

	public Action<OperatorDefinition, CodeWriter> GetEqualityOperatorBodyWriter() =>
		(def, operatorBlock) => operatorBlock
			.Append("return ")
			.Append(def.Parameters[0].Name)
			.Append(".Equals(")
			.Append(def.Parameters[1].Name)
			.AppendLine(");");

	public TypeDefinition AdjustUnionTypeDefinition(TypeDefinition typeDefinition)
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
					TypeName = TypeNames.Byte,
					Name = "Index",
				},
			],
			Methods =
			[
				.. typeDefinition.Methods,
				new MethodDefinition
				{
					Accessibility = Accessibility.Public,
					MethodModifier = MethodModifier.Override(),
					ReturnType = TypeNames.String(),
					Name = "ToString",
					BodyWriter = (_, methodBlock) =>
					{
						WriteCasesSwitchBody(
							methodBlock,
							(unionCase, caseParameters) =>
							{
								var caseStringRepresentation = UnionGenerationUtils.GetCaseStringRepresentation(
									unionCase.Name,
									unionCase.Parameters
										.Zip(caseParameters, (x, y) => (x.Name, y.ValueAccessor("this")))
										.ToArray());
								return $"return {caseStringRepresentation};";
							});

						methodBlock
							.AppendLine(UnionGenerationUtils.ThrowUnionInInvalidStateCode)
							.AppendLine("return null!;");
					},
				},
			],
		};
	}

	public IReadOnlyList<TypeDefinition> GetAdditionalTypes() =>
		_unionImplementationGenerator.GetAdditionalTypes();

	private void WriteCasesSwitchBody(
		CodeWriter codeWriter, Func<UnionCaseInfo, IReadOnlyList<CaseParameter>, string> caseStatementProvider)
	{
		codeWriter.AppendLine("switch (Index)");
		using var switchBlock = codeWriter.NewBlock();
		foreach (var unionCase in _union.Cases)
		{
			var caseParameters = _casesParameters[unionCase];
			var caseIndex = GetUnionCaseIndex(unionCase);
			switchBlock
				.AppendLine($"case {caseIndex}:")
				.Append("\t")
				.AppendLine(caseStatementProvider(unionCase, caseParameters));
		}
	}

	private int GetUnionCaseIndex(UnionCaseInfo unionCase) => _union.Cases.IndexOf(unionCase) + 1;

	private static string GetLayoutAttribute(LayoutKind layoutKind) =>
		$"global::System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.{layoutKind})";

	private readonly record struct CaseParameter(string FieldPath, TypeName TypeName, Func<string, string> ValueAccessor);

	private sealed class UnionImplementationGenerator
	{
		private const string BlittableStructFieldName = "UnionBlittableDataField";

		private readonly List<DefaultParameterInfo> _defaultParameters = [];
		private readonly Dictionary<UnionCaseInfo, CaseBlittableStructInfo> _blittableParameters = new();
		private readonly TypeInfo _unionBlittableDataTypeInfo;

		public UnionImplementationGenerator(TypeInfo unionBlittableDataTypeInfo)
		{
			_unionBlittableDataTypeInfo = unionBlittableDataTypeInfo;
		}

		public CaseParameter AddCaseParameter(UnionCaseParameterInfo caseParameter, UnionCaseInfo unionCase) =>
			caseParameter.TypeName.TypeInfo.Kind.Match(
				_ =>
				{
					var referenceTypeParameter = GetOrAddDefaultParameter(TypeNames.Object(), unionCase);
					return new CaseParameter(
						referenceTypeParameter.Name,
						caseParameter.TypeName,
						v => $"{TypeInfos.Unsafe}.As<{caseParameter.TypeName}>({v}.{referenceTypeParameter.Name})");
				},
				isUnmanaged =>
				{
					if (isUnmanaged && !caseParameter.ContainsGenericParameters)
					{
						if (!_blittableParameters.TryGetValue(unionCase, out var blittableStructInfo))
						{
							var typeInfo = TypeInfo.SpecificType(
								_unionBlittableDataTypeInfo.Namespace, _unionBlittableDataTypeInfo,
								$"{unionCase.Name}BlittableData", TypeInfo.TypeKind.ValueType(true));
							blittableStructInfo = new CaseBlittableStructInfo(typeInfo, $"{unionCase.Name}Data", []);
							_blittableParameters[unionCase] = blittableStructInfo;
						}

						blittableStructInfo.Fields.Add(new TypeNamePair(caseParameter.TypeName, caseParameter.Name));
						var fieldPath = $"{BlittableStructFieldName}.{blittableStructInfo.FieldName}.{caseParameter.Name}";
						return new CaseParameter(fieldPath, caseParameter.TypeName, v => $"{v}.{fieldPath}");
					}

					var defaultParameter = GetOrAddDefaultParameter(caseParameter.TypeName, unionCase);
					return new CaseParameter(defaultParameter.Name, caseParameter.TypeName, v => $"{v}.{defaultParameter.Name}");
				},
				() =>
				{
					var defaultParameter = GetOrAddDefaultParameter(new TypeName(caseParameter.TypeName.TypeInfo, false), unionCase);
					return new CaseParameter(defaultParameter.Name, caseParameter.TypeName, v => $"{v}.{defaultParameter.Name}");
				});

		public TypeDefinition AdjustUnionTypeDefinition(TypeDefinition typeDefinition)
		{
			var blittableDataField = _blittableParameters.Count > 0
				? new FieldDefinition
				{
					Accessibility = Accessibility.Private,
					TypeName = new TypeName(_unionBlittableDataTypeInfo, false),
					Name = BlittableStructFieldName,
				}
				: null;

			return typeDefinition with
			{
				Fields =
				[
					.. typeDefinition.Fields,
					.. _defaultParameters
						.Select(p => new FieldDefinition
						{
							Accessibility = Accessibility.Private,
							TypeName = p.Field.TypeName,
							Name = p.Field.Name,
						}),
					.. blittableDataField != null ? [blittableDataField] : Array.Empty<FieldDefinition>(),
				],
			};
		}

		public IReadOnlyList<TypeDefinition> GetAdditionalTypes()
		{
			var blittableDataNestedType = GenerateBlittableDataNestedType();
			return blittableDataNestedType != null ? [blittableDataNestedType] : [];
		}

		private TypeNamePair GetOrAddDefaultParameter(TypeName typeName, UnionCaseInfo unionCase)
		{
			var defaultParameter = _defaultParameters.Find(x => x.Field.TypeName == typeName && !x.UsedBy.Contains(unionCase));
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
				Accessibility = Accessibility.Internal,
				Kind = TypeKind.Struct(false),
				Name = _unionBlittableDataTypeInfo.Name,
				Attributes = [GetLayoutAttribute(LayoutKind.Explicit)],
				Fields = _blittableParameters.Values
					.Select(x => new FieldDefinition
					{
						Accessibility = Accessibility.Public,
						TypeName = new TypeName(x.StructInfo, false),
						Name = x.FieldName,
						Attributes = ["global::System.Runtime.InteropServices.FieldOffsetAttribute(0)"],
					})
					.ToArray(),
				NestedTypes = _blittableParameters.Values
					.Select(x => new TypeDefinition
					{
						Accessibility = Accessibility.Public,
						Kind = TypeKind.Struct(false),
						Name = x.StructInfo.Name,
						Attributes = [GetLayoutAttribute(LayoutKind.Auto)],
						Fields = x.Fields
							.Select(y => new FieldDefinition
							{
								Accessibility = Accessibility.Public,
								TypeName = y.TypeName,
								Name = y.Name,
							})
							.ToArray(),
					})
					.ToArray(),
			};
		}

		private readonly record struct DefaultParameterInfo(TypeNamePair Field, HashSet<UnionCaseInfo> UsedBy);

		private readonly record struct TypeNamePair(TypeName TypeName, string Name);

		private readonly record struct CaseBlittableStructInfo(TypeInfo StructInfo, string FieldName, List<TypeNamePair> Fields);
	}
}