#region References

using System;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Sample.Keystone;
using Cornerstone.Sample.Keystone.State;

#endregion

namespace Cornerstone.Sample.Tabs.Data;

/// <summary>
/// Projects mesh sync state onto the tab. The view binds these properties only.
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
[DependencyInjected]
public partial class TabSyncMeshViewModel : DispatchableViewModel
{
	#region Constructors

	[DependencyInjectionConstructor]
	public TabSyncMeshViewModel(AppBus bus, AppState state)
	{
		Bus = bus;
		TrackProperties(state.Mesh)
			.MapTwoWay(nameof(MeshFeatureState.CenterName))
			.MapTwoWay(nameof(MeshFeatureState.EastName))
			.MapTwoWay(nameof(MeshFeatureState.NorthName))
			.MapTwoWay(nameof(MeshFeatureState.SouthName))
			.MapTwoWay(nameof(MeshFeatureState.WestName))
			.MapOneWay(nameof(MeshFeatureState.CanRunWork))
			.MapOneWay(nameof(MeshFeatureState.CenterModifiedOn))
			.MapOneWay(nameof(MeshFeatureState.EastModifiedOn))
			.MapOneWay(nameof(MeshFeatureState.LastElapsed))
			.MapOneWay(nameof(MeshFeatureState.LastStatus))
			.MapOneWay(nameof(MeshFeatureState.NorthModifiedOn))
			.MapOneWay(nameof(MeshFeatureState.SouthModifiedOn))
			.MapOneWay(nameof(MeshFeatureState.WestModifiedOn));
	}

	#endregion

	#region Properties

	public AppBus Bus { get; }

	public partial bool CanRunWork { get; set; }

	public partial DateTime CenterModifiedOn { get; set; }

	public partial string CenterName { get; set; }

	public partial DateTime EastModifiedOn { get; set; }

	public partial string EastName { get; set; }

	public partial TimeSpan LastElapsed { get; set; }

	public partial string LastStatus { get; set; }

	public partial DateTime NorthModifiedOn { get; set; }

	public partial string NorthName { get; set; }

	public partial DateTime SouthModifiedOn { get; set; }

	public partial string SouthName { get; set; }

	public partial DateTime WestModifiedOn { get; set; }

	public partial string WestName { get; set; }

	#endregion
}
