#region References

using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Tabs;

/// <summary>
/// Host state for the Automatic demo (not itself dispatchable).
/// <see cref="Projection" /> is the DispatchableViewModel attached only when its View is shown.
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
public partial class TabAppDispatcherAutomaticViewModel : ViewModel
{
	#region Constructors

	[DependencyInjectionConstructor]
	public TabAppDispatcherAutomaticViewModel(Profiler profiler)
	{
		Model = new TabAppDispatcherTestModel();
		Projection = new TabAppDispatcherTestViewModel(Model, profiler);

		// Nested ContentControl starts attached — user deattaches the View explicitly.
		AttachedProjection = Projection;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Projection shown in the nested ContentControl when the user attaches the View.
	/// Null means detached (no View).
	/// </summary>
	[Notify]
	public partial TabAppDispatcherTestViewModel AttachedProjection { get; set; }

	public TabAppDispatcherTestModel Model { get; }

	/// <summary>
	/// Always-tracked projection; IsAttached follows nested View presence.
	/// </summary>
	public TabAppDispatcherTestViewModel Projection { get; }

	#endregion

	#region Methods

	public void AttachView()
	{
		AttachedProjection = Projection;
	}

	public void DetachView()
	{
		AttachedProjection = null;
	}

	#endregion
}