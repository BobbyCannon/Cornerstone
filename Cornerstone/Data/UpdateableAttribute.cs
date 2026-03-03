#region References

using System;

#endregion

namespace Cornerstone.Data;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class UpdateableAttribute : CornerstoneAttribute
{
	#region Constructors

	public UpdateableAttribute() : this(false)
	{
	}

	public UpdateableAttribute(bool invokeBase)
	{
		InvokeBase = invokeBase;
	}

	#endregion

	#region Properties

	public bool InvokeBase { get; set; }

	#endregion
}