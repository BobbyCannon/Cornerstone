#region References

using System.Threading.Tasks;
using Cornerstone.Extensions;
using Cornerstone.Net;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Storage;
using Microsoft.Maui.Storage;

#endregion

namespace Cornerstone.Maui;

public class MauiSecureVault : SecureVault
{
	#region Fields

	private string _keyName;
	private readonly RuntimeInformation _runtimeInformation;

	#endregion

	#region Constructors

	public MauiSecureVault(RuntimeInformation information, IDispatcher dispatcher)
		: base(information, dispatcher)
	{
		_runtimeInformation = information;
	}

	#endregion

	#region Properties

	private string KeyName => _keyName ??= GetVaultKey(nameof(Credential));

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void ClearCredential()
	{
		Credential.Reset();
		SecureStorage.Default.Remove(KeyName);
	}

	public override string GetVaultKey(string name)
	{
		return $"{_runtimeInformation.ApplicationName.Replace(" ", "")}.{name}";
	}

	/// <inheritdoc />
	public override async Task<bool> ReadCredentialAsync()
	{
		var value = await SecureStorage.Default.GetAsync(KeyName);
		var credential = value?.FromJson<Credential>();

		if ((credential == null) || (credential.SecurePassword.Length <= 0))
		{
			return false;
		}

		Credential.UpdateWith(credential);
		Credential.ResetHasChanges();

		return true;
	}

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
	public override bool TryWriteData<T>(string key, T data)
	{
		var storeKey = GetVaultKey(key);
		var response = data.ToJson();
		SecureStorage.Default.SetAsync(storeKey, response).AwaitResults();
		return true;
	}

	/// <inheritdoc />
	public override async Task<bool> WriteCredentialAsync()
	{
		var json = Credential?.ToRawJson();
		if (json == null)
		{
			return false;
		}

		await SecureStorage.Default.SetAsync(KeyName, json);
		return true;
	}

	#endregion
}