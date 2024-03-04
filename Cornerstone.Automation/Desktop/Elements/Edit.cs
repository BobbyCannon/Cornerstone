#region References

using Cornerstone.Automation.Desktop.Pattern;
using Interop.UIAutomationClient;

#endregion

namespace Cornerstone.Automation.Desktop.Elements;

/// <summary>
/// Represents a edit element.
/// </summary>
public class Edit : DesktopElement
{
	#region Constructors

	internal Edit(IUIAutomationElement element, Application application, DesktopElement parent)
		: base(element, application, parent)
	{
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets a value indicating whether the control can have a value set programmatically, or that can be edited by the user.
	/// </summary>
	public bool ReadOnly => ValuePattern.Create(this)?.IsReadOnly ?? true;

	/// <summary>
	/// Gets the text value.
	/// </summary>
	public string Text
	{
		get => GetText();
		set => SetText(value);
	}

	#endregion
}