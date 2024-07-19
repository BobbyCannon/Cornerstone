#region References

using System;
using System.Threading.Tasks;

#endregion

namespace Cornerstone.Security.Vault;

public class WindowsHelloServiceDummy : IWindowsHelloService
{
	#region Constructors

	public WindowsHelloServiceDummy()
	{
		IsSupported = false;
		HasBeenSetup = false;
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public bool HasBeenSetup { get; }

	/// <inheritdoc />
	public bool HasNotBeenSetup => !HasBeenSetup;

	/// <inheritdoc />
	public bool IsSupported { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public void DeleteVaultKeyAsync()
	{
	}

	/// <inheritdoc />
	public Task<byte[]> GetVaultKeyAsync()
	{
		return Task.FromResult(Array.Empty<byte>());
	}

	#endregion
}

public interface IWindowsHelloService
{
	#region Properties

	bool HasBeenSetup { get; }

	bool HasNotBeenSetup { get; }

	bool IsSupported { get; }

	#endregion

	#region Methods

	void DeleteVaultKeyAsync();

	Task<byte[]> GetVaultKeyAsync();

	#endregion
}