#region References

using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Cornerstone.Extensions;
using Cornerstone.Net;
using Cornerstone.Presentation;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Storage;

/// <summary>
/// The vault to store credential securely.
/// </summary>
public class SecureVault : Bindable
{
	#region Fields

	private readonly DirectoryInfo _dataDirectory;

	#endregion

	#region Constructors

	/// <summary>
	/// Create an instance of the web secure vault.
	/// </summary>
	/// <param name="information"> The runtime information. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public SecureVault(IRuntimeInformation information, IDispatcher dispatcher = null)
		: this(null, information.ApplicationDataLocation, dispatcher)
	{
	}

	/// <summary>
	/// Creates an instance of the vault.
	/// </summary>
	/// <param name="credential"> The default credential. Allows setting web or token as base credential type. </param>
	/// <param name="dataDirectory"> The data directory to store the vault entries. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	protected SecureVault(Credential credential, string dataDirectory, IDispatcher dispatcher) : base(dispatcher)
	{
		_dataDirectory = new DirectoryInfo(dataDirectory);

		if (credential == null)
		{
			Credential = new Credential(dispatcher);
			return;
		}

		var clone = credential.ShallowClone();
		clone.UpdateDispatcher(dispatcher);
		Credential = clone;
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
	/// Clears the credential from the vault.
	/// </summary>
	public virtual void ClearCredential()
	{
		var keyFileInfo = GetVaultKeyFilePath(nameof(Credential));
		keyFileInfo.SafeDelete();

		Credential.Reset();
	}

	/// <summary>
	/// Calculate a vault key by the provide name.
	/// </summary>
	/// <param name="name"> The name of the object being stored. </param>
	/// <returns> The vault key for the provided name. </returns>
	public virtual string GetVaultKey(string name)
	{
		return $"{name}.vault";
	}

	/// <summary>
	/// Get the file path of the vault key.
	/// </summary>
	/// <param name="name"> The name of the object being stored. </param>
	/// <returns> The vault key file info for the provided name. </returns>
	public FileInfo GetVaultKeyFilePath(string name)
	{
		return new FileInfo(Path.Combine(_dataDirectory.FullName, "Vault", GetVaultKey(name)));
	}

	/// <summary>
	/// Gets the stored credential from the vault.
	/// </summary>
	/// <returns> Return true if the credential was read otherwise false. </returns>
	public bool ReadCredential()
	{
		return ReadCredentialAsync().AwaitResults();
	}

	/// <summary>
	/// Gets the stored credential from the vault.
	/// </summary>
	/// <returns> Return true if the credential was read otherwise false. </returns>
	public virtual Task<bool> ReadCredentialAsync()
	{
		var result = TryReadData<Credential>(nameof(Credential), out var credential);
		if (!result)
		{
			return Task.FromResult(false);
		}

		Credential.UpdateWith(credential);

		return Task.FromResult(true);
	}

	/// <summary>
	/// Gets the stored data from the vault.
	/// </summary>
	/// <param name="name"> The name of the data to read. </param>
	/// <param name="data"> The data read if the key was found. </param>
	/// <returns> Return true if the data was read otherwise false. </returns>
	public virtual bool TryReadData<T>(string name, out T data)
	{
		var keyFileInfo = GetVaultKeyFilePath(name);
		if (!keyFileInfo.Exists)
		{
			data = default;
			return false;
		}

		try
		{
			var encrypted = File.ReadAllBytes(keyFileInfo.FullName);
			var jsonBytes = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
			var json = Encoding.Default.GetString(jsonBytes);
			data = json.FromJson<T>();
			return true;
		}
		catch
		{
			data = default;
			return false;
		}
	}

	/// <summary>
	/// Writes the data to the vault.
	/// </summary>
	/// <param name="name"> The name of the data to write. </param>
	/// <param name="data"> The data to write. </param>
	/// <returns> Return true if the data was written otherwise false. </returns>
	public virtual bool TryWriteData<T>(string name, T data)
	{
		try
		{
			var keyFileInfo = GetVaultKeyFilePath(name);
			keyFileInfo.Directory.SafeCreate();
			var json = data.ToJson();
			var jsonBytes = Encoding.Default.GetBytes(json);
			var encrypted = ProtectedData.Protect(jsonBytes, null, DataProtectionScope.CurrentUser);
			File.WriteAllBytes(keyFileInfo.FullName, encrypted);
			return true;
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Writes a credential to the vault.
	/// </summary>
	/// <returns> Return true if the credential was written otherwise false. </returns>
	public bool WriteCredential()
	{
		return WriteCredentialAsync().AwaitResults();
	}

	/// <summary>
	/// Writes a credential to the vault.
	/// </summary>
	/// <returns> Return true if the credential was written otherwise false. </returns>
	public virtual Task<bool> WriteCredentialAsync()
	{
		var json = Credential?.ToRawJson();
		return Task.FromResult((json != null) && TryWriteData(nameof(Credential), Credential));
	}

	#endregion
}