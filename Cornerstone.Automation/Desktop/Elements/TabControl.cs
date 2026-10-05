#region References

using Interop.UIAutomationClient;

#endregion

namespace Cornerstone.Automation.Desktop.Elements;

/// <summary>
/// Represents the tab control.
/// </summary>
public class TabControl : DesktopElement
{
	#region Constructors

	internal TabControl(IUIAutomationElement element, Application application, DesktopElement parent)
		: base(element, application, parent)
	{
	}

	#endregion
}