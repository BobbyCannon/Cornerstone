#region References

using System;
using System.Threading.Tasks;

#endregion

namespace Cornerstone.Security;

public class BiometricKeyServiceStub : IBiometricKeyService
{
	#region Constructors

	public BiometricKeyServiceStub()
	{
		DisplayName = string.Empty;
		IsSupported = false;
		HasBeenSetup = false;
		StaleKeyRemoved = false;
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public string DisplayName { get; }

	/// <inheritdoc />
	public bool HasBeenSetup { get; }

	/// <inheritdoc />
	public bool HasNotBeenSetup => BiometricKeyRules.HasNotBeenSetup(IsSupported, HasBeenSetup);

	/// <inheritdoc />
	public bool IsSupported { get; }

	/// <inheritdoc />
	public bool StaleKeyRemoved { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public Task DeleteVaultKeyAsync()
	{
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public Task<byte[]> GetVaultKeyAsync()
	{
		return Task.FromResult<byte[]>(null);
	}

	/// <inheritdoc />
	public void RememberChallenge()
	{
	}

	#endregion
}

/// <summary>
/// Stable device-owner secret used to wrap the vault master password.
/// A later unlock returns the same bytes while that platform key still exists.
/// </summary>
public interface IBiometricKeyService
{
	#region Properties

	/// <summary>
	/// Short name for buttons and errors. Empty when this device has no authenticator.
	/// </summary>
	string DisplayName { get; }

	/// <summary>
	/// True when the authenticator key exists. A locked or busy authenticator is still set up.
	/// </summary>
	bool HasBeenSetup { get; }

	/// <summary>
	/// True when an authenticator is available and no key exists.
	/// </summary>
	bool HasNotBeenSetup { get; }

	bool IsSupported { get; }

	/// <summary>
	/// True after GetVaultKeyAsync removes a key that cannot unwrap the vault.
	/// Setup calls GetVaultKeyAsync once more. Unlock does not.
	/// </summary>
	bool StaleKeyRemoved { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Removes the platform key and any saved secret. Await this so the caller does not block.
	/// </summary>
	Task DeleteVaultKeyAsync();

	/// <summary>
	/// Returns the vault secret. Null means the user cancelled or this attempt cannot unlock.
	/// Lockout and configuration failures throw, and the enrollment stays in place.
	/// The platform persists its secret or challenge before this task returns.
	/// </summary>
	Task<byte[]> GetVaultKeyAsync();

	/// <summary>
	/// Rewrites the Windows challenge file. GetVaultKeyAsync already persists that file.
	/// Android and iOS ignore this.
	/// </summary>
	void RememberChallenge();

	#endregion
}