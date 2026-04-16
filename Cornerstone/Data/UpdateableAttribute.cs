#region References

using System;

#endregion

namespace Cornerstone.Data;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class UpdateableAttribute : CornerstoneAttribute
{
	#region Constructors

	public UpdateableAttribute(UpdateableAction action, string[] properties)
	{
		Action = action;
		Properties = properties;
	}

	#endregion

	#region Properties

	public UpdateableAction Action { get; set; }

	public string[] Properties { get; set;  }

	#endregion
}