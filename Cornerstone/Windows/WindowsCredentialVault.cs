#region References

using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Security;

#endregion

namespace Cornerstone.Windows;

/// <summary>
/// A secure vault for windows applications.
/// </summary>
public class WindowsCredentialVault : CredentialVault
{
	#region Constructors

	/// <inheritdoc />
	public WindowsCredentialVault(IRuntimeInformation runtimeInformation, IDispatcher dispatcher)
		: base(runtimeInformation, dispatcher)
	{
	}

	#endregion

	#region Methods

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
			data = credential.Password.FromJson<T>() ?? default;
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
	public override bool TryWriteData<T>(string name, T data)
	{
		try
		{
			var vault = new PasswordVault();
			vault.Add(new PasswordCredential(GetVaultName(), name, data.ToJson()));
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