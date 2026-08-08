#region References

using System;

#endregion

namespace Cornerstone.Testing;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
public sealed class SkipInAotAttribute : Attribute
{
	#region Constructors

	public SkipInAotAttribute(string reason = "Not compatible with AOT")
	{
		Reason = reason;
	}

	#endregion

	#region Properties

	public string Reason { get; }

	#endregion
}