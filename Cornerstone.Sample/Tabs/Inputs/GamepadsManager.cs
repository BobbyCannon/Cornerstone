#region References

using System;
using System.Threading;
using Cornerstone.Input;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Threading;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Tabs.Inputs;

/// <summary>
/// Four gamepad slots for the Inputs sample. Uses the platform Gamepad when
/// registered (Windows XInput); otherwise GamepadStub.
/// </summary>
[DependencyInjected]
[SourceReflection]
public class GamepadsManager : Manager
{
	#region Fields

	private int _workerCount;
	private DispatcherTimer _worker;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public GamepadsManager(IDateTimeProvider dateTimeProvider, IDependencyProvider dependencyProvider)
	{
		Gamepad0 = CreateGamepad(dependencyProvider, dateTimeProvider, 0);
		Gamepad1 = CreateGamepad(dependencyProvider, dateTimeProvider, 1);
		Gamepad2 = CreateGamepad(dependencyProvider, dateTimeProvider, 2);
		Gamepad3 = CreateGamepad(dependencyProvider, dateTimeProvider, 3);
		Gamepads = [Gamepad0, Gamepad1, Gamepad2, Gamepad3];
	}

	#endregion

	#region Properties

	public Gamepad Gamepad0 { get; }

	public Gamepad Gamepad1 { get; }

	public Gamepad Gamepad2 { get; }

	public Gamepad Gamepad3 { get; }

	public Gamepad[] Gamepads { get; }

	public bool IsWorking => _worker != null;

	#endregion

	#region Methods

	public void StartWorking()
	{
		if (Interlocked.Increment(ref _workerCount) != 1)
		{
			return;
		}

		_worker = new DispatcherTimer(
			TimeSpan.FromMilliseconds(16),
			DispatcherPriority.Background,
			(_, _) => Update());
	}

	public void StopWorking()
	{
		var remaining = Interlocked.Decrement(ref _workerCount);
		if (remaining > 0)
		{
			return;
		}

		if (remaining < 0)
		{
			Interlocked.Exchange(ref _workerCount, 0);
		}

		_worker?.Stop();
		_worker = null;
	}

	public override void Update()
	{
		foreach (var gamepad in Gamepads)
		{
			gamepad.Update();
		}
	}

	private static Gamepad CreateGamepad(IDependencyProvider dependencyProvider, IDateTimeProvider dateTimeProvider, int index)
	{
		Gamepad gamepad;
		if (!dependencyProvider.IsSingleton(typeof(Gamepad))
			&& dependencyProvider.TryGetInstance(out gamepad))
		{
			gamepad.State.Index = index;
			return gamepad;
		}

		gamepad = new GamepadStub(dateTimeProvider);
		gamepad.State.Index = index;
		return gamepad;
	}

	#endregion
}
