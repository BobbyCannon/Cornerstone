#region References

using Interop.UIAutomationClient;

#endregion

namespace Cornerstone.Automation.Desktop.Elements;

/// <summary>
/// Represents the header.
/// </summary>
public class Header : DesktopElement
{
	#region Constructors

	internal Header(IUIAutomationElement element, Application application, DesktopElement parent)
		: base(element, application, parent)
	{
	}

	#endregion
}