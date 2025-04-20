#region References

using System;
using System.Threading.Tasks;
using Cornerstone.Collections;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Managers;
using Cornerstone.Security.SecurityKeys.Apdu;

#endregion

namespace Cornerstone.Security.SecurityKeys;

public abstract class SmartCardReader : Manager
{
	#region Constructors

	/// <inheritdoc />
	protected SmartCardReader(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion

	#region Properties

	public abstract ReadOnlySpeedyList<SelectionOption<string>> AvailableReaders { get; }

	public SecurityCard Card { get; protected set; }

	public SelectionOption<string> SelectedReader { get; set; }

	#endregion

	#region Methods

	public virtual void OnWriteLine(string e)
	{
		WriteLine?.Invoke(this, e);
	}

	public virtual Task RefreshReadersAsync()
	{
		return Task.CompletedTask;
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