#region References

using System;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Data;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class UpdateableAttribute : CornerstoneAttribute
{
	#region Constructors

	public UpdateableAttribute(UpdateableAction action, string[] properties, bool inherit = true)
	{
		Action = action;
		Properties = properties;
		Inherit = inherit;
	}

	#endregion

	#region Properties

	public UpdateableAction Action { get; set; }

	/// <summary>
	/// Only relevant for the "*" property.
	/// </summary>
	public bool Inherit { get; set; }

	public string[] Properties { get; set; }

	#endregion
}