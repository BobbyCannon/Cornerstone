#region References

using System.Threading.Tasks;
using Cornerstone.Net;
using Cornerstone.Presentation;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Security;

/// <summary>
/// The vault to store credential securely.
/// </summary>
public abstract class CredentialVault : Bindable
{
	#region Fields

	protected readonly IRuntimeInformation RuntimeInformation;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates an instance of the vault.
	/// </summary>
	/// <param name="runtimeInformation"> The runtime information. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	protected CredentialVault(IRuntimeInformation runtimeInformation, IDispatcher dispatcher) : base(dispatcher)
	{
		RuntimeInformation = runtimeInformation;
		Credential = new Credential(dispatcher);
	}

	#endregion

	#region Properties

	/// <summary>
	/// The credential for the vault.
	/// </summary>
	public Credential Credential { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Gets the stored credential from the vault.
	/// </summary>
	/// <returns> Return true if the credential was read otherwise false. </returns>
	public bool LoadCredential()
	{
		if (TryReadData<Credential>(nameof(Credential), out var data))
		{
			Credential.UpdateWith(data);
			return true;
		}

		return false;
	}

	/// <summary>
	/// Gets the stored credential from the vault.
	/// </summary>
	/// <returns> Return true if the credential was read otherwise false. </returns>
	public Task<bool> LoadCredentialAsync()
	{
		return Task.Run(LoadCredential);
	}

	/// <summary>
	/// Clears the credential from the vault.
	/// </summary>
	public bool RemoveCredential()
	{
		if (TryRemoveData(nameof(Credential)))
		{
			Credential.Reset();
			return true;
		}

		return false;
	}

	/// <summary>
	/// Writes a credential to the vault.
	/// </summary>
	/// <returns> Return true if the credential was written otherwise false. </returns>
	public bool SaveCredential()
	{
		return TryWriteData(nameof(Credential), Credential);
	}

	/// <summary>
	/// Writes a credential to the vault.
	/// </summary>
	/// <returns> Return true if the credential was written otherwise false. </returns>
	public Task<bool> SaveCredentialAsync()
	{
		return Task.Run(SaveCredential);
	}

	/// <summary>
	/// Gets the stored data from the vault.
	/// </summary>
	/// <param name="name"> The name of the data to read. </param>
	/// <param name="data"> The data read if the key was found. </param>
	/// <returns> Return true if the data was read otherwise false. </returns>
	public abstract bool TryReadData<T>(string name, out T data);

	/// <summary>
	/// Remove the data to the vault.
	/// </summary>
	/// <param name="name"> The name of the data to remove. </param>
	/// <returns> Return true if the data was removed otherwise false. </returns>
	public abstract bool TryRemoveData(string name);

	/// <summary>
	/// Writes the data to the vault.
	/// </summary>
	/// <param name="name"> The name of the data to write. </param>
	/// <param name="data"> The data to write. </param>
	/// <returns> Return true if the data was written otherwise false. </returns>
	public abstract bool TryWriteData<T>(string name, T data);

	/// <summary>
	/// Get the vault name.
	/// </summary>
	/// <returns> The name of the vault. </returns>
	protected virtual string GetVaultName()
	{
		return RuntimeInformation.ApplicationName.Replace(" ", "");
	}

	#endregion
}