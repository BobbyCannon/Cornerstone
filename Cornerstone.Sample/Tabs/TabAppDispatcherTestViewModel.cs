#region References

using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Tabs;

[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
public partial class TabAppDispatcherTestViewModel
	: DispatchableViewModel<TabAppDispatcherTestModel>,
		ITabAppDispatcherTestModel,
		IUpdateable<ITabAppDispatcherTestModel>
{
	#region Constructors

	public TabAppDispatcherTestViewModel() : this(new TabAppDispatcherTestModel(), new Profiler())
	{
	}

	[DependencyInjectionConstructor]
	public TabAppDispatcherTestViewModel(TabAppDispatcherTestModel model, Profiler profiler) : base(model)
	{
		Profiler = profiler;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Projected from the model on each AppDispatcher apply (View rate is system-scoped).
	/// </summary>
	public partial int Number { get; set; }

	public Profiler Profiler { get; }

	#endregion
}