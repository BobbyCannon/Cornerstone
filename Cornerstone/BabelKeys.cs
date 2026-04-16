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
	ArgumentTooSmall,
	IndexOutOfRange,
	IndexAndLengthOutOfRange,
	LengthCannotBeNegative,

	// Authentication / Authorization
	AuthenticationFailed,
	Unauthorized,

	// Security
	EncryptionFailed,
	DecryptionFailed,
	NewPasswordEntriesDoNotMatch,
	PasswordIsInvalid,

	// Security Key
	SecurityKeyNotFound,

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
	SyncManagerDisabled,
	SyncSessionAlreadyActive,
	SyncSessionInvalid,
	SyncSessionNotFound,
	SyncSettingsInvalid,

	// Clipboard
	ClipboardCouldNotBeOpened,
	ClipboardCouldNotBeClosed,
	ClipboardCouldNotBeCleared,
	ClipboardCouldNotBeRead,
	ClipboardCouldNotSetData,

	// Serialization
	SerializationFailedUnsupportedType,
	DeserializationFailedCrc,
	DeserializationFailedUnsupportedType,

	// Source Reflection
	SourceReflectionTypeNotDefined,

	// Testing
	TestingShouldBeNull,
	TestingShouldBeTrue,
	TestingShouldBeFalse,
	TestingShouldNotBeNull,
	TestingShouldHaveFailed
}