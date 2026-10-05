#region References

using System;
using Cornerstone.Presentation.Animation;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Animation;

internal class TestClock : IClock, IDisposable
{
	#region Fields

	private TimeSpan _curTime;

	private IObserver<TimeSpan> _observer;

	#endregion

	#region Properties

	public PlayState PlayState { get; set; } = PlayState.Run;

	#endregion

	#region Methods

	public void Dispose()
	{
		_observer?.OnCompleted();
	}

	public void Pulse(TimeSpan time)
	{
		_curTime += time;
		_observer?.OnNext(_curTime);
	}

	public void Step(TimeSpan time)
	{
		_observer?.OnNext(time);
	}

	public IDisposable Subscribe(IObserver<TimeSpan> observer)
	{
		_observer = observer;
		return this;
	}

	#endregion
}