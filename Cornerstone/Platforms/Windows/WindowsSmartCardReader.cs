#region References

using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Enumeration;
using Windows.Devices.SmartCards;
using Windows.Foundation.Metadata;
using Cornerstone.Attributes;
using Cornerstone.Presentation;
using Cornerstone.Security.SecurityKeys;
using Buffer = Windows.Storage.Streams.Buffer;
using SmartCardReader = Windows.Devices.SmartCards.SmartCardReader;

#endregion

namespace Cornerstone.Platforms.Windows;

public class WindowsSmartCardReader : Security.SecurityKeys.SmartCardReader
{
	#region Fields

	private SmartCardReader _reader;
	private SmartCard _smartCard;

	#endregion

	#region Constructors

	/// <inheritdoc />
	[DependencyInjectionConstructor]
	public WindowsSmartCardReader(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void Initialize()
	{
		LocateReader();
		InitializeCard();
		base.Initialize();
	}

	/// <inheritdoc />
	public override void Uninitialize()
	{
		_smartCard = null;
		if (_reader != null)
		{
			_reader.CardAdded -= ReaderCardAdded;
			_reader.CardRemoved -= ReaderCardRemoved;
		}
		_reader = null;
		base.Uninitialize();
	}

	/// <inheritdoc />
	protected override byte[] Transmit(byte[] command)
	{
		using var connection = _smartCard?.ConnectAsync().AwaitResults();
		if (connection == null)
		{
			return [];
		}

		var buffer = new Buffer((uint) command.Length);
		command.CopyTo(buffer);

		//OnWriteLine($">>> {BitConverter.ToString(buffer.ToArray())}");

		var result = connection.TransmitAsync(buffer).AwaitResults().ToArray();

		//OnWriteLine($"<<< {BitConverter.ToString(result)}");

		return result;
	}

	/// <summary>
	/// Try to initialize an existing card if found during initializing.
	/// </summary>
	private async void InitializeCard()
	{
		var reader = _reader;
		if ((reader == null) || (Card != null))
		{
			return;
		}

		var foundCards = await _reader.FindAllCardsAsync();
		var foundCard = foundCards.FirstOrDefault();
		if (foundCard == null)
		{
			return;
		}

		ProcessInsertedCard(foundCard);
	}

	private async void LocateReader()
	{
		if (_reader != null)
		{
			return;
		}

		// Get all smart card readers
		// Make sure we have the API we need
		if (!ApiInformation.IsTypePresent(typeof(SmartCardConnection).FullName))
		{
			return;
		}

		var readers = await DeviceInformation.FindAllAsync(SmartCardReader.GetDeviceSelector(SmartCardReaderKind.Nfc));
		
		if (readers.Count == 0)
		{
			readers = await DeviceInformation.FindAllAsync(SmartCardReader.GetDeviceSelector(SmartCardReaderKind.Any));
		}
		
		if (readers.Count == 0)
		{
			OnWriteLine("No smart card readers found.");
			return;
		}

		// Use the first reader for simplicity
		_reader = await SmartCardReader.FromIdAsync(readers[0].Id);

		if (_reader == null)
		{
			OnWriteLine("Failed to connect to the reader.");
			return;
		}

		_reader.CardAdded += ReaderCardAdded;
		_reader.CardRemoved += ReaderCardRemoved;

		OnWriteLine($"{_reader.Name} {_reader.Kind} was found...");
	}

	private void ProcessInsertedCard(SmartCard smartCard)
	{
		var atr = smartCard.GetAnswerToResetAsync().AwaitResults();
		var atrHex = BitConverter.ToString(atr.ToArray());

		switch (atrHex)
		{
			case MiFareClassicSecurityCardReader.CardId:
			{
				_smartCard = smartCard;

				var card = new MiFareClassicSecurityCardReader(this, GetDispatcher());
				card.Refresh();
				Card = card;
				OnCardInserted(card);
				break;
			}
		}
	}

	private void ReaderCardAdded(object sender, CardAddedEventArgs e)
	{
		ProcessInsertedCard(e.SmartCard);
	}

	private void ReaderCardRemoved(object sender, CardRemovedEventArgs e)
	{
		_smartCard = null;
		OnCardRemoved(Card);
		Card = null;
	}

	#endregion
}