#region References

using Cornerstone.Platforms.iOS.Internal;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Security.SecurityKeys;

#endregion

namespace Cornerstone.Platforms.iOS;

public class IOSSecurityCardReader : SecurityCardReader
{
	#region Fields

	private readonly InternalSecurityCardReader _implementation;

	#endregion

	#region Constructors

	/// <inheritdoc />
	[DependencyInjectionConstructor]
	public IOSSecurityCardReader(IDispatcher dispatcher) : base(dispatcher)
	{
		_implementation = new InternalSecurityCardReader();

		IsAvailable = false; //_implementation.IsAvailable;
		IsListening = false;
	}

	#endregion

	#region Methods

	public override void ReadCard()
	{
	}

	public override void StartListening()
	{
		_implementation?.StartListening();
	}

	public override void StopListening()
	{
		_implementation?.StopListening();
	}

	/// <inheritdoc />
	public override void UninitializeLifecycle()
	{
		RemoveCard();
		base.UninitializeLifecycle();
	}

	public override void WriteCard()
	{
	}

	#endregion
}