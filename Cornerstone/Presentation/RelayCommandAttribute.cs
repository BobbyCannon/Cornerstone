#region References

using System;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// Instruct Cornerstone.Generators to generate a relay command for the method.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class RelayCommandAttribute : CornerstoneAttribute
{
	#region Constructors

	/// <summary>
	/// Generate a property whose name is derived from the name of this field, with a public getter and setter
	/// </summary>
	public RelayCommandAttribute()
	{
	}

	#endregion

	#region Properties

	public string CanExecuteMethod { get; set; }

	#endregion
}