#region References

using System.Threading.Tasks;
using Android.Content;
using Android.Nfc;
using Android.Nfc.Tech;
using Android.OS;
using Cornerstone.Extensions;
using Cornerstone.Platforms.Android.Internal;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Security.SecurityKeys;
using Java.Lang;

#endregion

namespace Cornerstone.Platforms.Android;

public class AndroidSecurityCardReader : SecurityCardReader
{
	#region Fields

	private Tag _currentTag;
	private readonly InternalNfcAdapter _implementation;

	#endregion

	#region Constructors

	/// <inheritdoc />
	[DependencyInjectionConstructor]
	public AndroidSecurityCardReader(IPermissions permissions, IDispatcher dispatcher) : base(dispatcher)
	{
		_implementation = new InternalNfcAdapter(permissions);

		IsAvailable = true;
		IsListening = false;

		_implementation.OnTagListeningStatusChanged += ImplementationOnOnTagListeningStatusChanged;
	}

	#endregion

	#region Methods

	internal void HandleOnResume()
	{
		_implementation.HandleOnResume();
	}

	public bool OnHandleIntent(Intent intent)
	{
		if (intent is not { Action: NfcAdapter.ActionTagDiscovered })
		{
			return false;
		}

		Tag tag;

		if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
		{
			tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag, Class.FromType(typeof(Tag))) as Tag;
		}
		else
		{
			#pragma warning disable CA1422
			tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
			#pragma warning restore CA1422
		}

		if (tag == null)
		{
			return true;
		}

		_currentTag = tag;

		Card = ProcessMifareClassic(_currentTag);

		if (Card != null)
		{
			OnCardInserted(Card);
		}

		return true;
	}

	public override void ReadCard()
	{
		var card = Card;
		switch (card)
		{
			case MiFareClassicSecurityCard:
			{
				// Implement read
				break;
			}
		}
	}

	/// <inheritdoc />
	public override async Task RefreshAsync()
	{
		await RefreshCardAsync();
		await base.RefreshAsync();
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
		StopListening();
		RemoveCard();
		base.UninitializeLifecycle();
	}

	public override void WriteCard()
	{
		var card = Card;
		switch (card)
		{
			case MiFareClassicSecurityCard:
			{
				try
				{
					using var mifareTag = MifareClassic.Get(_currentTag);
					if (mifareTag == null)
					{
						throw new Exception(Babel.Tower[BabelKeys.SecurityKeyNotFound]);
					}

					if (!mifareTag.IsConnected)
					{
						mifareTag.Connect();
					}

					// Block 0 is read only, block 3 is the Sector's end
					if (mifareTag.AuthenticateSectorWithKeyA(0, MiFareClassicSecurityCard.KeyA) != true)
					{
						throw new Exception("Failed to authenticated card.");
					}
					mifareTag.WriteBlock(1, card.Data.SubArray(16, 16));
					mifareTag.WriteBlock(2, card.Data.SubArray(32, 16));

					// Sector 1
					if (mifareTag.AuthenticateSectorWithKeyA(1, MiFareClassicSecurityCard.KeyA) != true)
					{
						throw new Exception("Failed to authenticated card.");
					}
					mifareTag.WriteBlock(4, card.Data.SubArray(64, 16));
					mifareTag.WriteBlock(5, card.Data.SubArray(80, 16));
					mifareTag.Close();
				}
				catch (SecurityException)
				{
					RemoveCard();
					throw new Exception(Babel.Tower[BabelKeys.SecurityKeyNotFound]);
				}
				break;
			}
		}
	}

	/// <inheritdoc />
	protected override void RemoveCard()
	{
		_currentTag = null;
		base.RemoveCard();
	}

	private void ImplementationOnOnTagListeningStatusChanged(bool isListening)
	{
		IsListening = isListening;
	}

	private SecurityCard ProcessMifareClassic(Tag tag)
	{
		using var mifareTag = MifareClassic.Get(tag);

		if (mifareTag != null)
		{
			try
			{
				mifareTag.Connect();

				var id = mifareTag.Tag?.GetId();
				var response = new MiFareClassicSecurityCard(GetDispatcher());
				response.UpdateUniqueId(id);
				var sectorCount = mifareTag.SectorCount;
				byte block = 0;

				for (var i = 0; i < sectorCount; i++)
				{
					// Authenticate with a default key.
					if (!mifareTag.AuthenticateSectorWithKeyA(i, MiFareClassicSecurityCard.KeyA))
					{
						continue;
					}
					var blockCount = mifareTag.GetBlockCountInSector(i);

					for (var j = 0; j < blockCount; j++)
					{
						var blockIndex = mifareTag.SectorToBlock(i);

						// Read the block
						var data = mifareTag.ReadBlock(blockIndex + j);
						response.WriteData(block++, data);
					}
				}

				return response;
			}
			catch (Exception)
			{
				//OnWriteLine($"Error processing MIFARE Classic: {e.Message}");
			}
			finally
			{
				try
				{
					mifareTag.Close();
				}
				catch (Exception)
				{
					//OnWriteLine($"Error closing MIFARE Classic tag: {e.Message}");
				}
			}
		}

		//OnWriteLine("This tag is not MIFARE Classic.");
		return null;
	}

	private Task RefreshCardAsync()
	{
		var card = Card;
		switch (card)
		{
			case MiFareClassicSecurityCard:
			{
				using var mifareTag = MifareClassic.Get(_currentTag);

				try
				{
					if (mifareTag == null)
					{
						throw new Exception(Babel.Tower[BabelKeys.SecurityKeyNotFound]);
					}

					if (!mifareTag.IsConnected)
					{
						mifareTag.Connect();
						mifareTag.Close();
					}
				}
				catch (SecurityException)
				{
					// I think this is getting stuck on the dispatcher
					RemoveCard();
				}
				break;
			}
		}
		return Task.CompletedTask;
	}

	#endregion
}