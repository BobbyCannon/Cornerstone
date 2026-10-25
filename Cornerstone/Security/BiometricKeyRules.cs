#region References

using System;
using System.Security.Cryptography;

#endregion

namespace Cornerstone.Security;

/// <summary>
/// Result of opening a platform credential without creating one.
/// Only <see cref="NotFound" /> means the key material is gone.
/// </summary>
internal enum BiometricOpenStatus
{
	Success = 0,
	NotFound = 1,
	UserCanceled = 2,
	UserPrefersPassword = 3,
	CredentialAlreadyExists = 4,
	SecurityDeviceLocked = 5,
	UnknownError = 6
}

/// <summary>
/// Decisions that decide whether a vault enrollment is kept.
/// Platform services translate their native results into these checks.
/// </summary>
internal static class BiometricKeyRules
{
	#region Methods

	/// <summary>
	/// A timed Android keystore key can be used after any recent strong biometric, including the lock screen.
	/// A vault key must be per-use, biometric-strong, and invalidated when the biometric set changes.
	/// Duration zero and a negative duration are both per-use. A positive duration is a time window.
	/// </summary>
	public static bool AndroidKeyIsPerUse(
		bool userAuthenticationRequired,
		bool invalidatedByBiometricEnrollment,
		int keySizeBits,
		int validityDurationSeconds,
		int authenticationType,
		int biometricStrong,
		int deviceCredential)
	{
		if (!userAuthenticationRequired || !invalidatedByBiometricEnrollment || (keySizeBits != 256))
		{
			return false;
		}

		if (validityDurationSeconds > 0)
		{
			return false;
		}

		var strong = (authenticationType & biometricStrong) == biometricStrong;
		var device = (authenticationType & deviceCredential) != 0;
		return strong && !device;
	}

	public static bool HasBeenSetup(bool isSupported, bool keyMaterialExists)
	{
		return isSupported && keyMaterialExists;
	}

	public static bool HasNotBeenSetup(bool isSupported, bool hasBeenSetup)
	{
		return isSupported && !hasBeenSetup;
	}

	/// <summary>
	/// AuthFailed and Decode are also returned for a context mismatch. The biometric set changed
	/// only when a previously stored domain state differs from the state that just authenticated.
	/// A missing baseline does not delete the item.
	/// </summary>
	public static bool IosBiometrySetChanged(byte[] storedDomainState, byte[] currentDomainState)
	{
		if ((storedDomainState == null) || (storedDomainState.Length == 0))
		{
			return false;
		}

		if ((currentDomainState == null) || (currentDomainState.Length == 0))
		{
			return false;
		}

		return !CryptographicOperations.FixedTimeEquals(storedDomainState, currentDomainState);
	}

	public static bool KeyMaterialExists(BiometricOpenStatus status)
	{
		return status != BiometricOpenStatus.NotFound;
	}

	#endregion
}
