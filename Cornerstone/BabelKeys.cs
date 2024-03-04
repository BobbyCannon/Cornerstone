#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Cornerstone;

/// <summary>
/// Set of keys for babel.
/// </summary>
public enum BabelKeys
{
	Unknown = 0,
	GeneralError = 1,
	HttpBadRequest = 2,
	NotFound = 3,
	NotInitialized = 4,

	// Validation
	ArgumentInvalid = 5,
	ArgumentIsNull = 6,
	ArgumentOutOfRange = 7,
	IndexOutOfRange = 8,
	IndexAndLengthOutOfRange = 9,

	// Authentication
	AuthenticationFailed = 10,

	// Authorization
	Unauthorized = 11,

	// Security
	DecryptionFailed = 12,

	// Services
	TimeServiceLocked = 13,

	// Sync
	SyncOptionsInvalid = 14,
	SyncSessionAlreadyActive = 15,
	SyncSessionInvalid = 16,
	SyncClientNotSupported = 17
}