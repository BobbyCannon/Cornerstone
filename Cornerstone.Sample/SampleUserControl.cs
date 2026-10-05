#region References

using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Profiling;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Sample;

public class SampleUserControl : UserControl, IDispatchable
{
	#region Properties

	public Profiler Profiler { get; set; }

	#endregion

	#region Methods

	public IDispatcher GetDispatcher()
	{
		return Dispatcher;
	}

	#endregion
}