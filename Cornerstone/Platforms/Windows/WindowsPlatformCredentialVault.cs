#region References

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Windows.Security.Credentials;
using Cornerstone.Extensions;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Security;
using Cornerstone.Serialization;

#endregion

namespace Cornerstone.Platforms.Windows;

/// <summary>
/// A secure vault for windows applications.
/// </summary>
[SourceReflection]
public class WindowsPlatformCredentialVault : PlatformCredentialVault
{
	#region Constructors

	[DependencyInjectionConstructor]
	public WindowsPlatformCredentialVault(IRuntimeInformation runtimeInformation)
		: base(runtimeInformation)
	{
	}

	#endregion

	#region Methods

	[UnconditionalSuppressMessage("Aot", "IL3050", Justification = "Base TryReadData cannot be annotated; JSON parse of vault payloads.")]
	[UnconditionalSuppressMessage("Trim", "IL2026", Justification = "Base TryReadData cannot be annotated; JSON parse of vault payloads.")]
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

	[UnconditionalSuppressMessage("Aot", "IL3050", Justification = "Base TryWriteData cannot be annotated; JSON write of vault payloads.")]
	[UnconditionalSuppressMessage("Trim", "IL2026", Justification = "Base TryWriteData cannot be annotated; JSON write of vault payloads.")]
	public override bool TryWriteData<T>(string name, T data)
	{
		try
		{
			var vault = new PasswordVault();
			var encrypted = ProtectedData.Protect(Encoding.Unicode.GetBytes(data.ToJson()), null, DataProtectionScope.CurrentUser);
			vault.Add(new PasswordCredential(GetVaultName(), name, System.Convert.ToBase64String(encrypted)));
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