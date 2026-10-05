#region References

using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Nfc;
using Android.OS;
using Cornerstone.Runtime;
using Java.Lang;
using Exception = System.Exception;

#endregion

namespace Cornerstone.Platforms.Android.Internal;

public class InternalNfcAdapter
{
	#region Fields

	private readonly NfcConfiguration _configuration;
	private bool _isListening;
	private readonly NfcAdapter _nfcAdapter;
	private NfcBroadcastReceiver _nfcBroadcastReceiver;
	private readonly IPermissions _permissions;

	#endregion

	#region Constructors

	/// <summary>
	/// Default constructor
	/// </summary>
	/// <param name="permissions"> </param>
	public InternalNfcAdapter(IPermissions permissions)
	{
		_permissions = permissions;
		_nfcAdapter = NfcAdapter.GetDefaultAdapter(CurrentContext);
		_configuration = NfcConfiguration.GetDefaultConfiguration();
	}

	#endregion

	#region Properties

	/// <summary>
	/// Checks if the application has NFC permission.
	/// </summary>
	public bool HasPermission
	{
		get
		{
			if (_permissions.NearFieldCommunications != PermissionStatus.Granted)
			{
				return false;
			}
			return _nfcAdapter != null;
		}
	}

	/// <summary>
	/// Checks if NFC Feature is enabled
	/// </summary>
	public bool IsEnabled => HasPermission && _nfcAdapter.IsEnabled;

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
	private Context CurrentContext => AndroidPlatform.Activity ?? Application.Context;

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
		_configuration.Update(configuration);
	}

	/// <summary>
	/// Starts tags detection
	/// </summary>
	public void StartListening()
	{
		if ((_nfcAdapter == null) || !IsEnabled || (CurrentActivity == null))
		{
			return;
		}

		var intent = new Intent(CurrentActivity, CurrentActivity.GetType()).AddFlags(ActivityFlags.SingleTop);

		PendingIntentFlags pendingIntentFlags = 0;

		if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
		{
			pendingIntentFlags = PendingIntentFlags.Mutable;
		}

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
		if ((_nfcAdapter != null)
			&& (CurrentActivity != null)
			&& (_permissions.NearFieldCommunications == PermissionStatus.Granted))
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

	/// <summary>
	/// Called when NFC status has changed
	/// </summary>
	private void OnNfcStatusChange()
	{
		OnNfcStatusChangedInternal?.Invoke(IsEnabled);
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