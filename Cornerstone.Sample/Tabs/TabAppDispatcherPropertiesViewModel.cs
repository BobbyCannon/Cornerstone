#region References

using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Tabs;

/// <summary>
/// Projects selected model fields via TrackProperties (rename, convert, two-way).
/// Not a 1:1 DispatchableViewModel&lt;T&gt; — only mapped properties flow.
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
public partial class TabAppDispatcherPropertiesViewModel : DispatchableViewModel
{
	#region Constructors

	[DependencyInjectionConstructor]
	public TabAppDispatcherPropertiesViewModel(TabAppDispatcherPropertyMapModel model)
	{
		Model = model;

		TrackProperties(model)
			.MapTwoWay(nameof(TabAppDispatcherPropertyMapModel.Title))
			.MapTwoWay(nameof(TabAppDispatcherPropertyMapModel.Count), nameof(ItemCount))
			.MapOneWay(nameof(TabAppDispatcherPropertyMapModel.Ratio), nameof(RatioText),
				(double r) => $"{r:P0}");
	}

	#endregion

	#region Properties

	/// <summary>
	/// Two-way rename: model.Count ↔ ItemCount.
	/// </summary>
	public partial int ItemCount { get; set; }

	public TabAppDispatcherPropertyMapModel Model { get; }

	/// <summary>
	/// One-way convert: model.Ratio (0..1) → display string (e.g. "25%").
	/// </summary>
	public partial string RatioText { get; set; }

	/// <summary>
	/// Two-way same name: model.Title ↔ Title.
	/// </summary>
	public partial string Title { get; set; }

	#endregion
}