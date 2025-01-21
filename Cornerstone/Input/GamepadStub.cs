#region References

using System;
using System.Threading;
using Cornerstone.Attributes;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Input;

public class GamepadStub : Gamepad
{
	#region Constructors

	/// <inheritdoc />
	[DependencyInjectionConstructor]
	public GamepadStub(WeakEventManager weakEventManager, IDispatcher dispatcher)
		: base(weakEventManager, dispatcher)
	{
		WorkerDelay = 1000;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override void Work(TimeSpan elapsed)
	{
	}

	#endregion
}