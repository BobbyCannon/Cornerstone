#region References

using System;
using Cornerstone.Presentation.Layout;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

public sealed class StubLayoutManager : ILayoutManager
{
	#region Constructors

	public StubLayoutManager()
	{
		Calls = new StubCallLog();
	}

	#endregion

	#region Properties

	public StubCallLog Calls { get; }

	#endregion

	#region Methods

	public void Dispose()
	{
		Calls.Add(nameof(Dispose));
	}

	public void ExecuteInitialLayoutPass()
	{
		Calls.Add(nameof(ExecuteInitialLayoutPass));
	}

	public void ExecuteLayoutPass()
	{
		Calls.Add(nameof(ExecuteLayoutPass));
	}

	public void InvalidateArrange(Layoutable control)
	{
		Calls.Add(nameof(InvalidateArrange), control);
	}

	public void InvalidateMeasure(Layoutable control)
	{
		Calls.Add(nameof(InvalidateMeasure), control);
	}

	public void RegisterEffectiveViewportListener(Layoutable control)
	{
		Calls.Add(nameof(RegisterEffectiveViewportListener), control);
	}

	public void UnregisterEffectiveViewportListener(Layoutable control)
	{
		Calls.Add(nameof(UnregisterEffectiveViewportListener), control);
	}

	#endregion

	#region Events

	public event EventHandler LayoutUpdated
	{
		add => Calls.Add("add_LayoutUpdated", value);
		remove => Calls.Add("remove_LayoutUpdated", value);
	}

	#endregion
}