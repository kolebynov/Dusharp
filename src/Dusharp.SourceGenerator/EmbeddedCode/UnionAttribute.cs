#nullable enable
using System;

namespace Dusharp
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public sealed class UnionAttribute : Attribute
	{
	}
}