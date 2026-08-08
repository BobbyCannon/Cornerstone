using Cornerstone.Profiling;

namespace Cornerstone.Presentation;

public interface IAppDispatcher
{
	#region Properties

	/// <summary>
	/// Optional system profiler. When null, AppDispatcher does no profiling work.
	/// Assign to opt into scopes such as <see cref="ApplicationViewModel.ApplyScopeName" />.
	/// </summary>
	Profiler SystemProfiler { get; set; }

	#endregion

	#region Methods

	void Release(DispatchableViewModel dispatchableViewModel);
	void Track(DispatchableViewModel dispatchableViewModel);

	#endregion
}
