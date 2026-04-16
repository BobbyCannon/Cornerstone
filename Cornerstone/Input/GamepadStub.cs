#region References

using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Input;

public class GamepadStub : Gamepad
{
	#region Constructors

	public GamepadStub() : this(DateTimeProvider.RealTime)
	{
	}

	/// <inheritdoc />
	[DependencyInjectionConstructor]
	public GamepadStub(IDateTimeProvider dateTimeProvider) : base(dateTimeProvider)
	{
	}

	#endregion

	#region Methods

	public override void Update()
	{
	}

	#endregion
}