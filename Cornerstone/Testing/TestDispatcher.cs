#region References

using System;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Testing;

/// <summary>
/// Represents a test dispatcher
/// </summary>
public class TestDispatcher : IDispatcher
{
	#region Methods

	public bool CheckAccess()
	{
		return true;
	}

	public void Post(Action action, DispatcherPriority priority = default)
	{
	}

	public void VerifyAccess()
	{
	}

	#endregion
}