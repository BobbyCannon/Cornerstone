#region References

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Windows.Security.Credentials;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Security;

#endregion

namespace Cornerstone.Platforms.Windows;

/// <summary>
/// A secure vault for windows applications.
/// </summary>
public class WindowsPlatformCredentialVault : PlatformCredentialVault
{
	#region Constructors

	/// <inheritdoc />
	public WindowsPlatformCredentialVault(IRuntimeInformation runtimeInformation, IDispatcher dispatcher)
		: base(runtimeInformation, dispatcher)
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override bool TryReadData<T>(string name, out T data)
	{
		try
		{
			var vault = new PasswordVault();
			var resource = vault.FindAllByResource(GetVaultName());
			var credential = resource.FirstOrDefault(x => x.UserName == name);
			if (credential == null)
			{
				data = default;
				return false;
			}
			credential.RetrievePassword();
			var encrypted = credential.Password.FromBase64StringToByteArray();
			var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
			var json = Encoding.Unicode.GetString(decrypted);
			data = json.FromJson<T>() ?? default;
			return true;
		}

		catch (Exception)
		{
			// Ignore failures because it may be a "not found" exception
			data = default;
			return false;
		}
	}

	/// <inheritdoc />
	public override bool TryRemoveData(string name)
	{
		try
		{
			var vault = new PasswordVault();
			var resources = vault.FindAllByResource(GetVaultName());
			var toRemove = resources.Where(x => x.UserName == name).ToList();
			toRemove.ForEach(vault.Remove);
			return true;
		}
		catch
		{
			// Ignore failures because we are trying to log in, we'll try again later
			return false;
		}
	}

	/// <inheritdoc />
	public override bool TryWriteData<T>(string name, T data)
	{
		try
		{
			var vault = new PasswordVault();
			var encrypted = ProtectedData.Protect(Encoding.Unicode.GetBytes(data.ToJson()), null, DataProtectionScope.CurrentUser);
			vault.Add(new PasswordCredential(GetVaultName(), name, encrypted.ToBase64String()));
			return true;
		}
		catch (Exception)
		{
			// Ignore failures because it may be a "not found" exception
			data = default;
			return false;
		}
	}

	#endregion
}