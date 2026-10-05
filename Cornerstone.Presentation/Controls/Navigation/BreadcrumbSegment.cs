using Cornerstone.Presentation.Controls;

namespace Cornerstone.Presentation.Controls.Navigation;

public class BreadcrumbSegment
{
	#region Constructors

	public BreadcrumbSegment()
	{
		Title = string.Empty;
		Key = string.Empty;
	}

	public BreadcrumbSegment(string title, string key, bool isCurrent, bool showSeparator)
	{
		Title = title ?? string.Empty;
		Key = key ?? string.Empty;
		IsCurrent = isCurrent;
		ShowSeparator = showSeparator;
	}

	#endregion

	#region Properties

	public bool IsCurrent { get; set; }

	public string Key { get; set; }

	public bool ShowSeparator { get; set; }

	public string Title { get; set; }

	#endregion
}