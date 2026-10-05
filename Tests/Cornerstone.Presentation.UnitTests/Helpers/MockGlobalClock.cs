#region References

using System;
using Cornerstone.Presentation.Animation;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

internal class MockGlobalClock : ClockBase, IGlobalClock
{
	#region Methods

	public new void Pulse(TimeSpan systemTime)
	{
		base.Pulse(systemTime);
	}

	#endregion
}