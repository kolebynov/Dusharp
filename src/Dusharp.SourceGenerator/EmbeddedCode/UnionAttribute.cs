#nullable enable
using System;

namespace Dusharp
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	internal sealed class UnionAttribute : Attribute
	{
	}
}