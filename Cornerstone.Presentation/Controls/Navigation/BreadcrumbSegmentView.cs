namespace Cornerstone.Presentation.Controls.Navigation;

/// <summary>
/// One crumb as the trail shows it. Separator can differ from the source segment when the ellipsis sits in front.
/// </summary>
public class BreadcrumbSegmentView
{
	#region Constructors

	public BreadcrumbSegmentView(BreadcrumbSegment segment, bool showSeparator)
	{
		Segment = segment ?? new BreadcrumbSegment();
		ShowSeparator = showSeparator;
		Title = Segment.Title ?? string.Empty;
		IsCurrent = Segment.IsCurrent;
	}

	#endregion

	#region Properties

	public bool IsCurrent { get; }

	public BreadcrumbSegment Segment { get; }

	public bool ShowSeparator { get; }

	public string Title { get; }

	#endregion
}
