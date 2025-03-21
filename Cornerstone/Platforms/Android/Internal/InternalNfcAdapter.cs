#region References

using System;
using System.Linq;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Nfc;
using Android.Nfc.Tech;
using Android.OS;
using Java.Lang;
using Console = System.Console;
using Debug = System.Diagnostics.Debug;
using Exception = System.Exception;

#endregion

namespace Cornerstone.Platforms.Android.Internal;

public class InternalNfcAdapter
{
	#region Fields

	private bool _isListening;
	private readonly NfcAdapter _nfcAdapter;
	private NfcBroadcastReceiver _nfcBroadcastReceiver;

	#endregion

	#region Constructors

	/// <summary>
	/// Default constructor
	/// </summary>
	public InternalNfcAdapter()
	{
		_nfcAdapter = NfcAdapter.GetDefaultAdapter(CurrentContext);
		Configuration = NfcConfiguration.GetDefaultConfiguration();
	}

	#endregion

	#region Properties

	/// <summary>
	/// NFC configuration
	/// </summary>
	public NfcConfiguration Configuration { get; }

	/// <summary>
	/// Checks if NFC Feature is available
	/// </summary>
	public bool IsAvailable
	{
		get
		{
			if (CurrentContext.CheckCallingOrSelfPermission(Manifest.Permission.Nfc) != Permission.Granted)
			{
				return false;
			}
			return _nfcAdapter != null;
		}
	}

	/// <summary>
	/// Checks if NFC Feature is enabled
	/// </summary>
	public bool IsEnabled => IsAvailable && _nfcAdapter.IsEnabled;

	/// <summary>
	/// Checks if writing mode is supported
	/// </summary>
	public bool IsWritingTagSupported => NfcUtilities.IsWritingSupported();

	/// <summary>
	/// Current Android <see cref="Activity" />
	/// </summary>
	private Activity CurrentActivity => AndroidPlatform.Activity;

	/// <summary>
	/// Current Android <see cref="Context" />
	/// </summary>
	private Context CurrentContext => AndroidPlatform.ApplicationContext;

	#endregion

	#region Methods

	/// <summary>
	/// Update NFC configuration
	/// </summary>
	/// <param name="configuration">
	/// <see cref="NfcConfiguration" />
	/// </param>
	public void SetConfiguration(NfcConfiguration configuration)
	{
		Configuration.Update(configuration);
	}

	/// <summary>
	/// Starts tags detection
	/// </summary>
	public void StartListening()
	{
		if (_nfcAdapter == null)
		{
			return;
		}

		var intent = new Intent(CurrentActivity, CurrentActivity.GetType()).AddFlags(ActivityFlags.SingleTop);

		// We don't use MonoAndroid12.0 as target framework for easier backward compatibility:
		// MonoAndroid12.0 needs JDK 11.
		PendingIntentFlags pendingIntentFlags = 0;

		#if NET6_0_OR_GREATER
		if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
		{
			pendingIntentFlags = PendingIntentFlags.Mutable;
		}
		#else
		if ((int) Build.VERSION.SdkInt >= 31) //Android.OS.BuildVersionCodes.S
		{
			pendingIntentFlags = (PendingIntentFlags) 33554432; //PendingIntentFlags.Mutable
		}
		#endif

		var pendingIntent = PendingIntent.GetActivity(CurrentActivity, 0, intent, pendingIntentFlags);

		var ndefFilter = new IntentFilter(NfcAdapter.ActionNdefDiscovered);
		ndefFilter.AddDataType("*/*");

		var tagFilter = new IntentFilter(NfcAdapter.ActionTagDiscovered);
		tagFilter.AddCategory(Intent.CategoryDefault);

		var filters = new[] { ndefFilter, tagFilter };

		_nfcAdapter.EnableForegroundDispatch(CurrentActivity, pendingIntent, filters, null);

		_isListening = true;
		OnTagListeningStatusChanged?.Invoke(_isListening);
	}

	/// <summary>
	/// Stops tags detection
	/// </summary>
	public void StopListening()
	{
		if (_nfcAdapter != null)
		{
			_nfcAdapter.DisableForegroundDispatch(CurrentActivity);
		}

		_isListening = false;
		OnTagListeningStatusChanged?.Invoke(_isListening);
	}

	/// <summary>
	/// Handle Android OnResume
	/// </summary>
	internal void HandleOnResume()
	{
		// Android 10 fix:
		// If listening mode is already enable, we restart listening when activity is resumed
		if (_isListening)
		{
			StartListening();
		}
	}

	internal ITagInfo GetTagInfo(Tag tag)
	{
		if (tag == null)
		{
			return null;
		}

		// Example: just get the tag ID
		var tagId = tag.GetId();
		var idString = BitConverter.ToString(tagId).Replace("-", "");
		Debug.WriteLine($"Tag ID: {idString}");

		// You can also get technologies supported by the tag
		var techList = tag.GetTechList().ToList();
		foreach (var tech in techList)
		{
			Debug.WriteLine($"Tag Technology: {tech}");
		}

		return ProcessMifareClassic(tag);
	}

	/// <summary>
	/// Called when NFC status has changed
	/// </summary>
	private void OnNfcStatusChange()
	{
		OnNfcStatusChangedInternal?.Invoke(IsEnabled);
	}

	private ITagInfo ProcessMifareClassic(Tag tag)
	{
		using var mifareTag = MifareClassic.Get(tag);
		if (mifareTag != null)
		{
			try
			{
				mifareTag.Connect();

				var id = mifareTag.Tag?.GetId();
				var response = new TagInfo(id, false);
				var sectorCount = mifareTag.SectorCount;

				for (var i = 0; i < sectorCount; i++)
				{
					// Authenticate with a default key. In practice, you'd use known keys or ones you've learned.
					if (mifareTag.AuthenticateSectorWithKeyA(i, MifareClassic.KeyDefault.ToArray()))
					{
						var blockCount = mifareTag.GetBlockCountInSector(i);

						for (var j = 0; j < blockCount; j++)
						{
							var blockIndex = mifareTag.SectorToBlock(i);
							// Read the block
							var data = mifareTag.ReadBlock(blockIndex + j);
							Console.WriteLine($"Block {blockIndex + j} data: {BitConverter.ToString(data)}");
						}
					}
				}

				return response;
			}
			catch (Exception e)
			{
				Console.WriteLine($"Error processing MIFARE Classic: {e.Message}");
			}
			finally
			{
				try
				{
					mifareTag.Close();
				}
				catch (Exception e)
				{
					Console.WriteLine($"Error closing MIFARE Classic tag: {e.Message}");
				}
			}
		}
		else
		{
			Console.WriteLine("This tag is not MIFARE Classic.");
		}

		return null;
	}

	/// <summary>
	/// Register NFC Broadcast Receiver
	/// </summary>
	private void RegisterListener()
	{
		_nfcBroadcastReceiver = new NfcBroadcastReceiver(OnNfcStatusChange);
		CurrentContext?.RegisterReceiver(_nfcBroadcastReceiver, new IntentFilter(NfcAdapter.ActionAdapterStateChanged));
	}

	/// <summary>
	/// Unregister NFC Broadcast Receiver
	/// </summary>
	private void UnRegisterListener()
	{
		if (_nfcBroadcastReceiver == null)
		{
			return;
		}

		try
		{
			CurrentContext?.UnregisterReceiver(_nfcBroadcastReceiver);
		}
		catch (IllegalArgumentException ex)
		{
			throw new Exception("NFC Broadcast Receiver Error: " + ex.Message);
		}

		_nfcBroadcastReceiver.Dispose();
		_nfcBroadcastReceiver = null;
	}

	#endregion

	#region Events

	public event OnNfcStatusChangedEventHandler OnNfcStatusChanged
	{
		add
		{
			var wasRunning = OnNfcStatusChangedInternal != null;
			OnNfcStatusChangedInternal += value;
			if (!wasRunning && (OnNfcStatusChangedInternal != null))
			{
				RegisterListener();
			}
		}
		remove
		{
			var wasRunning = OnNfcStatusChangedInternal != null;
			OnNfcStatusChangedInternal -= value;
			if (wasRunning && (OnNfcStatusChangedInternal == null))
			{
				UnRegisterListener();
			}
		}
	}

	public event TagListeningStatusChangedEventHandler OnTagListeningStatusChanged;
	private event OnNfcStatusChangedEventHandler OnNfcStatusChangedInternal;

	#endregion

	#region Classes

	/// <summary>
	/// Broadcast Receiver to check NFC feature availability
	/// </summary>
	[BroadcastReceiver(Enabled = true, Exported = false, Label = "NFC Status Broadcast Receiver")]
	private class NfcBroadcastReceiver : BroadcastReceiver
	{
		#region Fields

		private readonly Action _onChanged;

		#endregion

		#region Constructors

		public NfcBroadcastReceiver()
		{
		}

		public NfcBroadcastReceiver(Action onChanged)
		{
			_onChanged = onChanged;
		}

		#endregion

		#region Methods

		public override async void OnReceive(Context context, Intent intent)
		{
			if (intent?.Action != NfcAdapter.ActionAdapterStateChanged)
			{
				return;
			}

			var state = intent.GetIntExtra(NfcAdapter.ExtraAdapterState, default);
			if ((state == NfcAdapter.StateOff) || (state == NfcAdapter.StateOn))
			{
				// await 1500ms to ensure that the status updates
				await Task.Delay(1500);
				_onChanged?.Invoke();
			}
		}

		#endregion
	}

	#endregion

	#region Delegates

	public delegate void NdefMessagePublishedEventHandler(ITagInfo tagInfo);

	public delegate void NdefMessageReceivedEventHandler(ITagInfo tagInfo);

	public delegate void OnNfcStatusChangedEventHandler(bool isEnabled);

	public delegate void TagDiscoveredEventHandler(ITagInfo tagInfo, bool format);

	public delegate void TagListeningStatusChangedEventHandler(bool isListening);

	#endregion
}