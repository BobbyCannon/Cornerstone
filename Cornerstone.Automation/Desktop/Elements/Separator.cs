#region References

using Interop.UIAutomationClient;

#endregion

namespace Cornerstone.Automation.Desktop.Elements;

/// <summary>
/// Represents the separator element.
/// </summary>
public class Separator : DesktopElement
{
	#region Constructors

	internal Separator(IUIAutomationElement element, Application application, DesktopElement parent)
		: base(element, application, parent)
	{
	}

	#endregion
}