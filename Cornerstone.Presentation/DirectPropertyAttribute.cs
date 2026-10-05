#region References

using System;

#endregion

namespace Cornerstone.Presentation;

[AttributeUsage(AttributeTargets.Property)]
public sealed class DirectPropertyAttribute : Attribute
{
	#region Constructors

	public DirectPropertyAttribute()
	{
	}

	/// <summary>
	/// Forwards the Cornerstone direct property to a view model property of the given name
	/// (typically nameof on the view model) and raises the control property when that value changes.
	/// </summary>
	public DirectPropertyAttribute(string viewModelProperty)
	{
		ForwardToViewModel = true;
		ViewModelProperty = viewModelProperty;
	}

	#endregion

	#region Properties

	/// <summary>
	/// When true, get/set the view model property and raise the Cornerstone property so
	/// control-name bindings (for example #Terminal.AutoScroll) update.
	/// Uses <see cref="ViewModelProperty"/> when set; otherwise the control property name.
	/// </summary>
	public bool ForwardToViewModel { get; set; }

	/// <summary>
	/// View model property to read and write. Defaults to the control property name when
	/// <see cref="ForwardToViewModel"/> is true.
	/// </summary>
	public string ViewModelProperty { get; set; }

	#endregion
}
