#region References

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Security;
using Foundation;
using LocalAuthentication;
using Security;

#endregion

namespace Cornerstone.Platforms.iOS;

/// <summary>
/// Keychain secret gated by the device biometric. The bytes are random and stored once.
/// Secure Enclave signatures are not used because each ECDSA signature is different and could not unwrap the vault.
/// </summary>
[Preserve(AllMembers = true)]
[SourceReflection]
[DependencyInjected]
[DependencyInjected(typeof(IBiometricKeyService))]
public class IOSBiometricKeyService : IBiometricKeyService
{
	#region Constants

	private const string AccountName = "BiometricVaultKey";
	private const string DomainStateFileName = "Biometric.domainstate";
	private const string PromptReason = "Unlock the vault";

	#endregion

	#region Fields

	private readonly Func<string> _directory;
	private string _displayName;
	private readonly object _gate;
	private bool? _isSupported;
	private Task<byte[]> _pendingUnlock;
	private readonly string _service;
	private bool _staleKeyRemoved;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public IOSBiometricKeyService(IRuntimeInformation runtimeInformation)
	{
		_gate = new object();
		_pendingUnlock = null;
		_staleKeyRemoved = false;
		_displayName = string.Empty;
		_directory = () => runtimeInformation.ApplicationDataLocation;
		var applicationName = runtimeInformation.ApplicationName;
		_service = string.IsNullOrWhiteSpace(applicationName) ? "Cornerstone" : applicationName;
	}

	#endregion

	#region Properties

	public string DisplayName
	{
		get
		{
			EnsureSupport();
			return _displayName;
		}
	}

	public bool HasBeenSetup
	{
		get
		{
			if (!IsSupported)
			{
				return false;
			}

			// Unknown means the keychain could not answer. That is not proof the item is gone.
			return ItemIsPresent();
		}
	}

	public bool HasNotBeenSetup => IsSupported && !HasBeenSetup;

	public bool StaleKeyRemoved => _staleKeyRemoved;

	public bool IsSupported
	{
		get
		{
			EnsureSupport();
			return _isSupported == true;
		}
	}

	#endregion

	#region Methods

	public Task DeleteVaultKeyAsync()
	{
		return Task.Run(() =>
		{
			var status = DeleteItem();
			DeleteDomainState();
			if ((status != SecStatusCode.Success) && (status != SecStatusCode.ItemNotFound))
			{
				throw new InvalidOperationException("The biometric vault key could not be deleted.");
			}
		});
	}

	public Task<byte[]> GetVaultKeyAsync()
	{
		lock (_gate)
		{
			if (_pendingUnlock is { IsCompleted: false })
			{
				return _pendingUnlock;
			}

			_pendingUnlock = ReadVaultKeyAsync();
			return _pendingUnlock;
		}
	}

	/// <inheritdoc />
	public void RememberChallenge()
	{
	}

	private bool CreateItem(LAContext context, out bool created)
	{
		created = false;
		var access = new SecAccessControl(
			SecAccessible.WhenPasscodeSetThisDeviceOnly,
			SecAccessControlCreateFlags.BiometryCurrentSet
		);
		if (access == null)
		{
			return false;
		}

		var secret = RandomNumberGenerator.GetBytes(32);
		try
		{
			using var record = new SecRecord(SecKind.GenericPassword);
			record.Service = _service;
			record.Account = AccountName;
			record.Synchronizable = false;
			record.AccessControl = access;
			record.AuthenticationContext = context;
			record.ValueData = NSData.FromArray(secret);
			var status = SecKeyChain.Add(record);
			if (status != SecStatusCode.Success)
			{
				return status == SecStatusCode.DuplicateItem;
			}

			created = true;
			return true;
		}
		finally
		{
			CryptographicOperations.ZeroMemory(secret);
		}
	}

	private SecStatusCode DeleteItem()
	{
		using var record = new SecRecord(SecKind.GenericPassword)
		{
			Service = _service,
			Account = AccountName,
			Synchronizable = false
		};
		return SecKeyChain.Remove(record);
	}

	private void EnsureSupport()
	{
		if (_isSupported == true)
		{
			return;
		}

		try
		{
			using var context = new LAContext();
			var supported = context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out _);
			_isSupported = supported ? true : null;
			_displayName = supported ? "Biometrics" : string.Empty;
		}
		catch (Exception)
		{
			_isSupported = null;
			_displayName = string.Empty;
		}
	}

	private bool BiometrySetChanged(LAContext context, SecStatusCode status)
	{
		if (status is not (SecStatusCode.AuthFailed or SecStatusCode.Decode))
		{
			return false;
		}

		return BiometricKeyRules.IosBiometrySetChanged(ReadStoredDomainState(), DomainStateBytes(context?.DomainState?.Biometry?.StateHash));
	}

	private void DeleteDomainState()
	{
		var path = DomainStatePath();
		if (string.IsNullOrEmpty(path) || !File.Exists(path))
		{
			return;
		}

		try
		{
			File.Delete(path);
		}
		catch (Exception)
		{
			// The keychain item is already gone. A leftover baseline only affects the next comparison.
		}
	}

	private string DomainStatePath()
	{
		var directory = _directory?.Invoke();
		if (string.IsNullOrEmpty(directory))
		{
			return null;
		}

		return Path.Combine(directory, DomainStateFileName);
	}

	private static byte[] DomainStateBytes(NSData state)
	{
		if ((state == null) || (state.Length == 0))
		{
			return null;
		}

		return state.ToArray();
	}

	/// <summary>
	/// SecItemCopyMatching on the main thread deadlocks: the keychain reply is delivered there.
	/// </summary>
	private bool ItemIsPresent()
	{
		if (!NSThread.IsMain)
		{
			return QueryItem() != false;
		}

		var captured = SynchronizationContext.Current;
		SynchronizationContext.SetSynchronizationContext(null);
		try
		{
			return Task.Run(() => QueryItem() != false).GetAwaiter().GetResult();
		}
		finally
		{
			SynchronizationContext.SetSynchronizationContext(captured);
		}
	}

	/// <summary>
	/// True when the item is present, false when it is absent, and null when the keychain did not answer.
	/// </summary>
	private bool? QueryItem()
	{
		try
		{
			using var context = new LAContext();
			context.InteractionNotAllowed = true;
			using var query = new SecRecord(SecKind.GenericPassword);
			query.Service = _service;
			query.Account = AccountName;
			query.Synchronizable = false;
			query.AuthenticationContext = context;
			using var match = SecKeyChain.QueryAsRecord(query, out var status);
			if (status is SecStatusCode.Success or SecStatusCode.InteractionNotAllowed)
			{
				return true;
			}

			if (status == SecStatusCode.ItemNotFound)
			{
				return false;
			}

			return null;
		}
		catch (Exception)
		{
			return null;
		}
	}

	private byte[] ReadOrCreateSecret(LAContext context)
	{
		var secret = ReadSecret(context, out var status);
		if (secret is { Length: > 0 })
		{
			RememberDomainState(context.DomainState?.Biometry?.StateHash);
			return secret;
		}

		if (BiometrySetChanged(context, status))
		{
			DeleteItem();
			DeleteDomainState();
			_staleKeyRemoved = true;
			return null;
		}

		if (status == SecStatusCode.UserCanceled)
		{
			return null;
		}

		if ((status != SecStatusCode.ItemNotFound) && (status != SecStatusCode.Success))
		{
			return null;
		}

		if (!CreateItem(context, out var created))
		{
			return null;
		}

		secret = ReadSecret(context, out status);
		if (secret is { Length: > 0 })
		{
			RememberDomainState(context.DomainState?.Biometry?.StateHash);
			return secret;
		}

		// This call created an item it cannot read back. An older item is left in place.
		if (created || BiometrySetChanged(context, status))
		{
			DeleteItem();
			DeleteDomainState();
			if (!created)
			{
				_staleKeyRemoved = true;
			}
		}

		return null;
	}

	private byte[] ReadStoredDomainState()
	{
		var path = DomainStatePath();
		if (string.IsNullOrEmpty(path) || !File.Exists(path))
		{
			return null;
		}

		try
		{
			var stored = File.ReadAllBytes(path);
			return (stored == null) || (stored.Length == 0) ? null : stored;
		}
		catch (Exception)
		{
			return null;
		}
	}

	private void RememberDomainState(NSData state)
	{
		var bytes = DomainStateBytes(state);
		var path = DomainStatePath();
		if ((bytes == null) || string.IsNullOrEmpty(path))
		{
			return;
		}

		var directory = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(directory))
		{
			Directory.CreateDirectory(directory);
		}

		var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
		try
		{
			File.WriteAllBytes(temporaryPath, bytes);
			File.Move(temporaryPath, path, true);
		}
		catch (Exception)
		{
			try
			{
				File.Delete(temporaryPath);
			}
			catch (Exception)
			{
				// The unlock already has the secret. A missed baseline must not fail it.
			}
		}
	}

	private byte[] ReadSecret(LAContext context, out SecStatusCode status)
	{
		using var query = new SecRecord(SecKind.GenericPassword);
		query.Service = _service;
		query.Account = AccountName;
		query.Synchronizable = false;
		query.AuthenticationContext = context;
		using var match = SecKeyChain.QueryAsRecord(query, out status);
		if ((status == SecStatusCode.Success) && (match?.ValueData != null))
		{
			return match.ValueData.ToArray();
		}

		return null;
	}

	private async Task<byte[]> ReadVaultKeyAsync()
	{
		_staleKeyRemoved = false;
		if (!IsSupported)
		{
			return null;
		}

		using var context = new LAContext();
		var (authenticated, error) = await context.EvaluatePolicyAsync(
			LAPolicy.DeviceOwnerAuthenticationWithBiometrics,
			PromptReason).ConfigureAwait(false);
		if (!authenticated)
		{
			ThrowIfHardFailure(error);
			return null;
		}

		return ReadOrCreateSecret(context);
	}

	private static void ThrowIfHardFailure(NSError error)
	{
		if (error == null)
		{
			return;
		}

		var code = (LAStatus) (int) error.Code;
		var description = error.LocalizedDescription;
		if (code is LAStatus.UserCancel or LAStatus.SystemCancel or LAStatus.AppCancel or LAStatus.UserFallback)
		{
			return;
		}

		if (code == LAStatus.BiometryLockout)
		{
			throw new InvalidOperationException(string.IsNullOrEmpty(description)
				? "Biometrics is locked. Try again later."
				: description);
		}

		if (!string.IsNullOrEmpty(description) && description.Contains("NSFaceIDUsageDescription", StringComparison.Ordinal))
		{
			throw new InvalidOperationException(description);
		}

		if (code is LAStatus.BiometryNotAvailable or LAStatus.BiometryNotEnrolled or LAStatus.PasscodeNotSet)
		{
			if (string.IsNullOrEmpty(description))
			{
				description = "Biometrics is not available.";
			}

			if ((code == LAStatus.BiometryNotAvailable) && !description.Contains("NSFaceIDUsageDescription", StringComparison.Ordinal))
			{
				description += " Face ID requires NSFaceIDUsageDescription in Info.plist.";
			}

			throw new InvalidOperationException(description);
		}

		if (!string.IsNullOrEmpty(description))
		{
			throw new InvalidOperationException(description);
		}
	}

	#endregion
}