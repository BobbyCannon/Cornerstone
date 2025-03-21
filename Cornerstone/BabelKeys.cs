

namespace Cornerstone;

/// <summary>
/// Set of keys for babel.
/// </summary>
public enum BabelKeys
{
	Unknown,
	GeneralError,
	HttpBadRequest,
	NotFound,
	NotInitialized,

	// Validation
	ArgumentInvalid,
	ArgumentIsNull,
	ArgumentOutOfRange,
	IndexOutOfRange,
	IndexAndLengthOutOfRange,

	// Authentication / Authorization
	AuthenticationFailed,
	Unauthorized,

	// Security
	EncryptionFailed,
	DecryptionFailed,
	NewPasswordEntriesDoNotMatch,
	PasswordIsInvalid,
	
	// Secure Vault
	VaultCredentialNotFound,
	VaultCredentialUserNameRequired,
	VaultCredentialPasswordRequired,
	VaultIsAlreadyOpen,
	VaultIsNotOpen,

	// Services
	DateTimeProviderLocked,
	DependencyProviderLocked,

	// Sync
	SyncClientNotSupported,
	SyncDeviceNotFound,
	SyncEntityIncorrectType,
	SyncSessionAlreadyActive,
	SyncSessionInvalid,
	SyncSessionNotFound,
	SyncSettingsInvalid,

	// Clipboard
	ClipboardCouldNotBeOpened,
	ClipboardCouldNotBeClosed,
	ClipboardCouldNotBeCleared,
	ClipboardCouldNotBeRead,
	ClipboardCouldNotSetData
}