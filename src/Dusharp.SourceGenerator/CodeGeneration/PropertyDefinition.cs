using System;
using System.Collections.Generic;
using Dusharp.CodeAnalyzing;
using Microsoft.CodeAnalysis;

namespace Dusharp.CodeGeneration;

public sealed partial class PropertyDefinition
{
	public Accessibility? Accessibility { get; init; }

	public bool IsStatic { get; init; }

	public required TypeName TypeName { get; init; }

	public required string Name { get; init; }

	public PropertyAccessor? Getter { get; init; }

	public PropertyAccessor? Setter { get; init; }

	public string? Initializer { get; init; }

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

	[Union]
	public partial struct PropertyAccessorImpl
	{
		[UnionCase]
		public static partial PropertyAccessorImpl Auto();

		[UnionCase]
		public static partial PropertyAccessorImpl Bodied(Action<PropertyDefinition, CodeWriter> bodyWriter);
	}
}