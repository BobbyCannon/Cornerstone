namespace Cornerstone.Platforms.Android.Internal;

/// <summary>
/// NFC Configuration class
/// </summary>
public class NfcConfiguration
{
	#region Properties

	/// <summary>
	/// Sets ISO 639-1 Language Code for all ndef records (default is "en")
	/// </summary>
	public string DefaultLanguageCode { get; set; }

	/// <summary>
	/// List of user defined messages
	/// </summary>
	public UserDefinedMessages Messages { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Get the default Nfc configuration
	/// </summary>
	/// <returns> Default <see cref="NfcConfiguration" /> </returns>
	public static NfcConfiguration GetDefaultConfiguration()
	{
		return new() { Messages = new UserDefinedMessages(), DefaultLanguageCode = "en" };
	}

	/// <summary>
	/// Update Nfc Configuration with a new configuration object
	/// </summary>
	/// <param name="newCfg">
	/// <see cref="NfcConfiguration" />
	/// </param>
	public void Update(NfcConfiguration newCfg)
	{
		if ((newCfg == null) || (newCfg.Messages == null))
		{
			return;
		}
		Messages = newCfg.Messages;
		DefaultLanguageCode = newCfg.DefaultLanguageCode;
	}

	#endregion
}

/// <summary>
/// User defined UI messages
/// </summary>
public class UserDefinedMessages
{
	#region Fields

	private string _nfcDialogAlertMessage = "Please hold your phone near a NFC tag";
	private string _nfcErrorCapacityTag = "Tag's capacity is too low";
	private string _nfcErrorEmptyTag = "Tag is empty";
	private string _nfcErrorMissingTag = "Tag is missing";
	private string _nfcErrorMissingTagInfo = "No Tag Informations: nothing to write";
	private string _nfcErrorNotCompliantTag = "Tag is not NDEF compliant";
	private string _nfcErrorNotSupportedTag = "Tag is not supported";
	private string _nfcErrorRead = "Read error. Please try again";
	private string _nfcErrorReadOnlyTag = "Tag is not writable";
	private string _nfcErrorWrite = "Nothing to write";
	private string _nfcSessionInvalidated = "Session Invalidated";
	private string _nfcSessionInvalidatedButton = "OK";
	private string _nfcSessionTimeout = "session timeout";
	private string _nfcSuccessClear = "Clear Operation Successful";
	private string _nfcSuccessRead = "Read Operation Successful";
	private string _nfcSuccessWrite = "Write Operation Successful";
	private string _nfcWritingNotSupported = "Writing NFC Tag is not supported on this device";

	#endregion

	#region Properties

	/// <summary>
	/// [iOS] NFC Scan dialog alert message
	/// </summary>
	public string NFCDialogAlertMessage
	{
		get => _nfcDialogAlertMessage;
		set
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				_nfcDialogAlertMessage = value;
			}
		}
	}

	/// <summary>
	/// Tag capacity error
	/// </summary>
	public string NFCErrorCapacityTag
	{
		get => _nfcErrorCapacityTag;
		set
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				_nfcErrorCapacityTag = value;
			}
		}
	}

	/// <summary>
	/// Empty tag error
	/// </summary>
	public string NFCErrorEmptyTag
	{
		get => _nfcErrorEmptyTag;
		set
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				_nfcErrorEmptyTag = value;
			}
		}
	}

	/// <summary>
	/// Missing tag error
	/// </summary>
	public string NFCErrorMissingTag
	{
		get => _nfcErrorMissingTag;
		set
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				_nfcErrorMissingTag = value;
			}
		}
	}

	/// <summary>
	/// Missing tag info error
	/// </summary>
	public string NFCErrorMissingTagInfo
	{
		get => _nfcErrorMissingTagInfo;
		set
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				_nfcErrorMissingTagInfo = value;
			}
		}
	}

	/// <summary>
	/// Not NDEF compliant tag error
	/// </summary>
	public string NFCErrorNotCompliantTag
	{
		get => _nfcErrorNotCompliantTag;
		set
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				_nfcErrorNotCompliantTag = value;
			}
		}
	}

	/// <summary>
	/// Not supported tag error
	/// </summary>
	public string NFCErrorNotSupportedTag
	{
		get => _nfcErrorNotSupportedTag;
		set
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				_nfcErrorNotSupportedTag = value;
			}
		}
	}

	/// <summary>
	/// Read operation error
	/// </summary>
	public string NFCErrorRead
	{
		get => _nfcErrorRead;
		set
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				_nfcErrorRead = value;
			}
		}
	}

	/// <summary>
	/// Read-only tag error
	/// </summary>
	public string NFCErrorReadOnlyTag
	{
		get => _nfcErrorReadOnlyTag;
		set
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				_nfcErrorReadOnlyTag = value;
			}
		}
	}

	/// <summary>
	/// Write operation error
	/// </summary>
	public string NFCErrorWrite
	{
		get => _nfcErrorWrite;
		set
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				_nfcErrorWrite = value;
			}
		}
	}

	/// <summary>
	/// Session invalidated
	/// </summary>
	public string NFCSessionInvalidated
	{
		get => _nfcSessionInvalidated;
		set
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				_nfcSessionInvalidated = value;
			}
		}
	}

	/// <summary>
	/// Session invalidated
	/// </summary>
	public string NFCSessionInvalidatedButton
	{
		get => _nfcSessionInvalidatedButton;
		set
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				_nfcSessionInvalidatedButton = value;
			}
		}
	}

	/// <summary>
	/// Session timeout
	/// </summary>
	public string NFCSessionTimeout
	{
		get => _nfcSessionTimeout;
		set
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				_nfcSessionTimeout = value;
			}
		}
	}

	/// <summary>
	/// [iOS] Successful clear operation message
	/// </summary>
	public string NFCSuccessClear
	{
		get => _nfcSuccessClear;
		set
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				_nfcSuccessClear = value;
			}
		}
	}

	/// <summary>
	/// [iOS] Successful read operation message
	/// </summary>
	public string NFCSuccessRead
	{
		get => _nfcSuccessRead;
		set
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				_nfcSuccessRead = value;
			}
		}
	}

	/// <summary>
	/// [iOS] Successful write operation message
	/// </summary>
	public string NFCSuccessWrite
	{
		get => _nfcSuccessWrite;
		set
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				_nfcSuccessWrite = value;
			}
		}
	}

	/// <summary>
	/// Writing feature not supported
	/// </summary>
	public string NFCWritingNotSupported
	{
		get => _nfcWritingNotSupported;
		set
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				_nfcWritingNotSupported = value;
			}
		}
	}

	#endregion
}