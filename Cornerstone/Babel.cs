#region References

using System;
using System.Collections.Concurrent;

#endregion

namespace Cornerstone;

/// <summary>
/// A singleton to handle application localization.
/// </summary>
public class Babel
{
	#region Fields

	private ConcurrentDictionary<string, string> _currentDictionary;
	private ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _dictionaries;

	#endregion

	#region Constructors

	static Babel()
	{
		Tower = new Babel();
	}

	private Babel()
	{
		Reset();
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets a string by name.
	/// </summary>
	/// <param name="key"> The name of the value. </param>
	public string this[string key] => _currentDictionary.TryGetValue(key, out var value) ? value : string.Empty;

	/// <summary>
	/// Gets a string by a custom key.
	/// </summary>
	/// <param name="key"> The custom key of the value. </param>
	public string this[Enum key] => _currentDictionary.TryGetValue(key.ToString(), out var value) ? value : string.Empty;

	/// <summary>
	/// Gets a string by key.
	/// </summary>
	/// <param name="key"> The babel key of the value. </param>
	public string this[BabelKeys key] => _currentDictionary.TryGetValue(key.ToString(), out var value) ? value : string.Empty;

	/// <summary>
	/// The current language.
	/// </summary>
	public string Language { get; private set; }

	/// <summary>
	/// The static instance of Babel.
	/// </summary>
	public static Babel Tower { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Try to add a key to the current dictionary. If found the existing entry will be updated.
	/// </summary>
	/// <param name="key"> The key to process. </param>
	/// <param name="value"> The value to be set. </param>
	/// <returns> </returns>
	public void AddOrUpdate(string key, string value)
	{
		_currentDictionary.AddOrUpdate(key, _ => value, (_, _) => value);
	}

	/// <summary>
	/// Try to add a key to the current dictionary. If found the existing entry will be updated.
	/// </summary>
	/// <param name="key"> The key to process. </param>
	/// <param name="value"> The value to be set. </param>
	/// <returns> </returns>
	public void AddOrUpdate(Enum key, string value)
	{
		_currentDictionary.AddOrUpdate(key.ToString(), _ => value, (_, _) => value);
	}

	/// <summary>
	/// Try to add a key to a language dictionary. If found the existing entry will be updated.
	/// </summary>
	/// <param name="language"> The language to update. </param>
	/// <param name="key"> The key to process. </param>
	/// <param name="value"> The value to be set. </param>
	/// <returns> </returns>
	public void AddOrUpdate(string language, string key, string value)
	{
		var dictionary = _dictionaries.GetOrAdd(language, _ => new ConcurrentDictionary<string, string>());
		dictionary.AddOrUpdate(key, _ => value, (_, _) => value);
	}

	/// <summary>
	/// Try to add a key to a language dictionary. If found the existing entry will be updated.
	/// </summary>
	/// <param name="language"> The language to update. </param>
	/// <param name="key"> The key to process. </param>
	/// <param name="value"> The value to be set. </param>
	/// <returns> </returns>
	public void AddOrUpdate(string language, BabelKeys key, string value)
	{
		var dictionary = _dictionaries.GetOrAdd(language, _ => new ConcurrentDictionary<string, string>());
		dictionary.AddOrUpdate(key.ToString(), _ => value, (_, _) => value);
	}

	/// <summary>
	/// Change dictionaries to another language.
	/// </summary>
	/// <param name="language"> The language to change to. </param>
	public void ChangeDictionary(string language)
	{
		Language = language;
		_currentDictionary = _dictionaries.GetOrAdd(language, _ => new ConcurrentDictionary<string, string>());
	}

	/// <summary>
	/// Check to see if the current dictionary contains the provided key
	/// </summary>
	/// <param name="key"> The key to test for. </param>
	/// <returns> True if the key exist otherwise false. </returns>
	public bool ContainsKey(string key)
	{
		return _currentDictionary.ContainsKey(key);
	}

	/// <summary>
	/// Check to see if the current dictionary contains the provided key
	/// </summary>
	/// <param name="key"> The key to test for. </param>
	/// <returns> True if the key exist otherwise false. </returns>
	public bool ContainsKey(BabelKeys key)
	{
		return _currentDictionary.ContainsKey(key.ToString());
	}

	/// <summary>
	/// Reset the babel tower back to defaults.
	/// </summary>
	public void Reset()
	{
		_dictionaries = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

		ChangeDictionary("English");

		AddOrUpdate("English", BabelKeys.GeneralError, "There is a issue with the system. We will get someone to look into this.");
		AddOrUpdate("English", BabelKeys.HttpBadRequest, "The requested is a bad request.");
		AddOrUpdate("English", BabelKeys.NotFound, "The requested resource could not be found.");
		AddOrUpdate("English", BabelKeys.NotInitialized, "The service not initialized so it is not ready for use.");

		// Validation
		AddOrUpdate("English", BabelKeys.ArgumentInvalid, "The argument is invalid.");
		AddOrUpdate("English", BabelKeys.ArgumentIsNull, "The argument is null but is required.");
		AddOrUpdate("English", BabelKeys.ArgumentOutOfRange, "The argument is out of range.");
		AddOrUpdate("English", BabelKeys.IndexOutOfRange, "The index is out of range.");
		AddOrUpdate("English", BabelKeys.IndexAndLengthOutOfRange, "The index + length is out of range.");

		// Authentication / Authorization
		AddOrUpdate("English", BabelKeys.AuthenticationFailed, "The credentials failed to authenticate. Please try again.");
		AddOrUpdate("English", BabelKeys.Unauthorized, "The request does not have sufficient privileges for this operation.");

		// Encryption
		AddOrUpdate("English", BabelKeys.EncryptionFailed, "The value was not able to be encrypted.");
		AddOrUpdate("English", BabelKeys.DecryptionFailed, "The value was not able to be decrypted.");
		AddOrUpdate("English", BabelKeys.NewPasswordEntriesDoNotMatch, "The new password values do not match.");
		AddOrUpdate("English", BabelKeys.PasswordIsInvalid, "The password is not correct.");

		// Secure Vault
		AddOrUpdate("English", BabelKeys.VaultCredentialNotFound, "The vault credential could not be found.");
		AddOrUpdate("English", BabelKeys.VaultCredentialUserNameRequired, "The vault credential UserName is required.");
		AddOrUpdate("English", BabelKeys.VaultCredentialPasswordRequired, "The vault credential Password is required.");
		AddOrUpdate("English", BabelKeys.VaultIsAlreadyOpen, "The vault is already open.");
		AddOrUpdate("English", BabelKeys.VaultIsNotOpen, "The vault is not currently open.");

		// Services
		AddOrUpdate("English", BabelKeys.DateTimeProviderLocked, "The date and time service has been locked and cannot change.");

		// Sync
		AddOrUpdate("English", BabelKeys.SyncOptionsInvalid, "The sync values are invalid.");
		AddOrUpdate("English", BabelKeys.SyncSessionAlreadyActive, "The sync session is already active.");
		AddOrUpdate("English", BabelKeys.SyncSessionInvalid, "The sync session is invalid.");
		AddOrUpdate("English", BabelKeys.SyncClientNotSupported, "The sync client is not supported.");

		// Clipboard
		AddOrUpdate("English", BabelKeys.ClipboardCouldNotBeOpened, "The clipboard could not be opened.");
		AddOrUpdate("English", BabelKeys.ClipboardCouldNotBeClosed, "The clipboard could not be closed.");
		AddOrUpdate("English", BabelKeys.ClipboardCouldNotBeCleared, "The clipboard could not be cleared.");
		AddOrUpdate("English", BabelKeys.ClipboardCouldNotBeRead, "The clipboard could not be read.");
		AddOrUpdate("English", BabelKeys.ClipboardCouldNotSetData, "The clipboard could not be written.");
	}

	#endregion
}