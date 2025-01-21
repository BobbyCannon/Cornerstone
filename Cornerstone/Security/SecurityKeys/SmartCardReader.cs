#region References

using System;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Managers;
using Cornerstone.Security.SecurityKeys.Apdu;

#endregion

namespace Cornerstone.Security.SecurityKeys;

public abstract class SmartCardReader : Manager
{
	#region Constructors

	/// <inheritdoc />
	protected SmartCardReader(WeakEventManager weakEventManager, IDispatcher dispatcher)
		: base(dispatcher)
	{
		WeakEventManager = weakEventManager;
	}

	#endregion

	#region Properties

	public SecurityCard Card { get; protected set; }

	public WeakEventManager WeakEventManager { get; }

	#endregion

	#region Methods

	public virtual void OnWriteLine(string e)
	{
		WriteLine?.Invoke(this, e);
	}

	public ApduResponse Transmit(ApduCommand command)
	{
		var apduBuffer = command.ToByteArray();
		var response = Transmit(apduBuffer);
		return new ApduResponse(response);
	}

	protected virtual void OnCardInserted(SecurityCard e)
	{
		CardInserted?.Invoke(this, e);
	}

	protected virtual void OnCardRemoved(SecurityCard e)
	{
		CardRemoved?.Invoke(this, e);
	}

	protected abstract byte[] Transmit(byte[] command);

	#endregion

	#region Events

	public event EventHandler<SecurityCard> CardInserted;
	public event EventHandler<SecurityCard> CardRemoved;
	public event EventHandler<string> WriteLine;

	#endregion
}