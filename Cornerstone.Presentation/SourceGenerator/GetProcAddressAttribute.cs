#region References

using System;

#endregion

namespace Cornerstone.Presentation.SourceGenerator;

[AttributeUsage(AttributeTargets.Method)]
internal sealed class GetProcAddressAttribute : Attribute
{
	#region Constructors

	public GetProcAddressAttribute(string proc)
	{
	}

	public GetProcAddressAttribute(string proc, bool optional = false)
	{
	}

	public GetProcAddressAttribute(bool optional)
	{
	}

	public GetProcAddressAttribute()
	{
	}

	#endregion
}