#region References

using System;
using Cornerstone.Attributes;
using Cornerstone.Input;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Platforms.Browser;

/// <summary>
/// https://w3c.github.io/gamepad/
/// </summary>
public class BrowserGamepad : Gamepad
{
	#region Constructors

	/// <inheritdoc />
	[DependencyInjectionConstructor]
	public BrowserGamepad(IDispatcher dispatcher) : base(dispatcher)
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