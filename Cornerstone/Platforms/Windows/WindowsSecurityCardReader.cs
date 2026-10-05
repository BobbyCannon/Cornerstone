#region References

using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SmartCards;
using Windows.Foundation.Metadata;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Security.SecurityKeys;
using Buffer = Windows.Storage.Streams.Buffer;
using SmartCardReader = Windows.Devices.SmartCards.SmartCardReader;

#endregion

namespace Cornerstone.Platforms.Windows;

public class WindowsSecurityCardReader : ApduSecurityCardReader
{
	#region Fields

	private readonly PresentationList<SmartCardReader> _allReaders;
	private SmartCard _currentCard;
	private SmartCardReader _currentReader;

	#endregion

	#region Constructors

	/// <inheritdoc />
	[DependencyInjectionConstructor]
	public WindowsSecurityCardReader(IDispatcher dispatcher) : base(dispatcher)
	{
		_allReaders = new PresentationList<SmartCardReader>();

		WeakEventManager.AddPresentationListUpdated<PresentationList<SmartCardReader>, SmartCardReader, WindowsSecurityCardReader>(_allReaders, this, FoundReadersOnListUpdated);

		IsAvailable = false;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void InitializeLifecycle()
	{
		_ = RefreshReadersAsync();
		base.InitializeLifecycle();
	}

	/// <inheritdoc />
	public override async Task RefreshAsync()
	{
		await RefreshReadersAsync();
		await base.RefreshAsync();
	}

	/// <inheritdoc />
	public override void UninitializeLifecycle()
	{
		_allReaders.Clear();
		_currentReader = null;
		_currentCard = null;
		base.UninitializeLifecycle();
	}

	protected override void RemoveCard()
	{
		_currentCard = null;
		_currentReader = null;
		base.RemoveCard();
	}

	protected override byte[] Transmit(byte[] command)
	{
		using var connection = _currentCard?.ConnectAsync().AwaitResults();
		if (connection == null)
		{
			return [];
		}

		var buffer = new Buffer((uint) command.Length);
		command.CopyTo(buffer);

		var result = connection.TransmitAsync(buffer).AwaitResults().ToArray();
		return result;
	}

	private void FoundReadersOnListUpdated(object sender, PresentationListUpdatedEventArg<SmartCardReader> e)
	{
		if (e.Removed != null)
		{
			foreach (var reader in e.Removed)
			{
				reader.CardAdded -= ReaderCardAdded;
				reader.CardRemoved -= ReaderCardRemoved;
			}
		}

		if (e.Added != null)
		{
			foreach (var reader in e.Added)
			{
				reader.CardAdded += ReaderCardAdded;
				reader.CardRemoved += ReaderCardRemoved;
			}
		}
	}

	private void ReaderCardAdded(object sender, CardAddedEventArgs e)
	{
		var reader = (SmartCardReader) sender;
		var atr = e.SmartCard.GetAnswerToResetAsync().AwaitResults();
		var atrHex = BitConverter.ToString(atr.ToArray());

		// Remove current card if one found
		RemoveCard();

		switch (atrHex)
		{
			case MiFareClassicSecurityCard.CardId:
			{
				_currentReader = reader;
				_currentCard = e.SmartCard;

				var card = new MiFareClassicSecurityCard(GetDispatcher());
				Card = card;
				ReadCard();
				Card.UpdateUniqueId(card.Data.SubArray(0, 4));
				break;
			}
			case MiFareUltralightSecurityCard.CardId:
			{
				_currentReader = reader;
				_currentCard = e.SmartCard;

				var card = new MiFareUltralightSecurityCard(GetDispatcher());
				Card = card;
				ReadCard();
				Card.UpdateUniqueId(card.Data.SubArray(0, 4));
				break;
			}
			default:
			{
				Card = new UnknownSecurityCard();
				break;
			}
		}

		OnCardInserted(Card);
	}

	private void ReaderCardRemoved(object sender, CardRemovedEventArgs e)
	{
		if (((SmartCardReader) sender == _currentReader)
			&& (e.SmartCard == _currentCard))
		{
			RemoveCard();
		}
	}

	private async Task RefreshReadersAsync()
	{
		// Get all smart card readers
		// Make sure we have the API we need
		if (!ApiInformation.IsTypePresent(typeof(SmartCardConnection).FullName))
		{
			IsAvailable = false;
			return;
		}

		var foundReaders = await DeviceInformation.FindAllAsync(SmartCardReader.GetDeviceSelector(SmartCardReaderKind.Any));
		if (foundReaders.Count == 0)
		{
			IsAvailable = false;
			return;
		}

		var readersToRemove = _allReaders.Where(x => foundReaders.All(r => r.Id != x.DeviceId)).ToList();
		var readersToAdd = foundReaders.Where(x => _allReaders.All(r => r.DeviceId != x.Id)).ToList();
		readersToRemove.ForEach(_allReaders.Remove);

		foreach (var item in readersToAdd)
		{
			var reader = await SmartCardReader.FromIdAsync(item.Id);
			_allReaders.Add(reader);
		}

		IsAvailable = _allReaders.Count > 0;
		IsListening = IsAvailable;
	}

	#endregion
}