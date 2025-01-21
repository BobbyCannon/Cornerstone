#region References

using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Security;
using Microsoft.Maui.Storage;

#endregion

namespace Cornerstone.Android;

public class AndroidCredentialVault : CredentialVault
{
	#region Constructors

	public AndroidCredentialVault(IRuntimeInformation runtimeInformation, IDispatcher dispatcher)
		: base(runtimeInformation, dispatcher)
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override bool TryReadData<T>(string key, out T data)
	{
		var storeKey = GetVaultKey(key);
		var value = SecureStorage.Default.GetAsync(storeKey).AwaitResults();

		if (string.IsNullOrEmpty(value))
		{
			data = default;
			return false;
		}

		var response = value.FromJson<T>();
		data = response;
		return true;
	}

	/// <inheritdoc />
	public override bool TryRemoveData(string name)
	{
		var keyName = GetVaultKey(name);
		return SecureStorage.Default.Remove(keyName);
	}

	/// <inheritdoc />
	public override bool TryWriteData<T>(string key, T data)
	{
		var storeKey = GetVaultKey(key);
		var response = data.ToJson();
		SecureStorage.Default.SetAsync(storeKey, response).AwaitResults();
		return true;
	}

	/// <summary>
	/// Calculate a vault key by the provide name.
	/// </summary>
	/// <param name="name"> The name of the object being stored. </param>
	/// <returns> The vault key for the provided name. </returns>
	private string GetVaultKey(string name)
	{
		return $"{GetVaultName()}.{name}";
	}

	#endregion
}