#region References

using System;
using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Keystone.State;

[SourceReflection]
[Notifiable(["*"])]
[DependencyInjected]
public partial class MeshFeatureState : CornerstoneObject
{
	#region Constructors

	[DependencyInjectionConstructor]
	public MeshFeatureState()
	{
		CanRunWork = false;
		CenterModifiedOn = DateTime.MinValue;
		CenterName = string.Empty;
		EastModifiedOn = DateTime.MinValue;
		EastName = string.Empty;
		IsAvailable = false;
		LastElapsed = TimeSpan.Zero;
		LastStatus = "Not started.";
		NorthModifiedOn = DateTime.MinValue;
		NorthName = string.Empty;
		SouthModifiedOn = DateTime.MinValue;
		SouthName = string.Empty;
		WestModifiedOn = DateTime.MinValue;
		WestName = string.Empty;
	}

	#endregion

	#region Properties

	public partial bool CanRunWork { get; set; }

	public partial DateTime CenterModifiedOn { get; set; }

	public partial string CenterName { get; set; }

	public partial DateTime EastModifiedOn { get; set; }

	public partial string EastName { get; set; }

	public partial bool IsAvailable { get; set; }

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
