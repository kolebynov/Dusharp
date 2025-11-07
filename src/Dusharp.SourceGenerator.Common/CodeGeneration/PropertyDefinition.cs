using Dusharp.SourceGenerator.Common.CodeAnalyzing;
using Microsoft.CodeAnalysis;

namespace Dusharp.SourceGenerator.Common.CodeGeneration;

public sealed partial class PropertyDefinition
{
	public Accessibility? Accessibility { get; init; }

	public bool IsStatic { get; init; }

	public required TypeName TypeName { get; init; }

	public required string Name { get; init; }

	public PropertyAccessor? Getter { get; init; }

	public PropertyAccessor? Setter { get; init; }

	public Action<CodeWriter>? InitializerWriter { get; init; }

	public IReadOnlyList<string> Attributes { get; init; } = [];

	public readonly struct PropertyAccessor
	{
		public Accessibility? Accessibility { get; }

		public PropertyAccessorImpl Impl { get; }

		public PropertyAccessor(Accessibility? accessibility, PropertyAccessorImpl impl)
		{
			Accessibility = accessibility;
			Impl = impl;
		}
	}

	public abstract record PropertyAccessorImpl
	{
		private PropertyAccessorImpl()
		{
		}

		public sealed record Auto : PropertyAccessorImpl;

		public sealed record Bodied(Action<PropertyDefinition, CodeWriter> BodyWriter) : PropertyAccessorImpl;
	}
}