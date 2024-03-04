#region References

using Interop.UIAutomationClient;

#endregion

namespace Cornerstone.Automation.Desktop.Elements;

/// <summary>
/// Represents the data item.
/// </summary>
public class DataItem : DesktopElement
{
	#region Constructors

	internal DataItem(IUIAutomationElement element, Application application, DesktopElement parent)
		: base(element, application, parent)
	{
	}

	#endregion
}