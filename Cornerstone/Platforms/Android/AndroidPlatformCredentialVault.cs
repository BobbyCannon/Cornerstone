#region References

using System.Diagnostics.CodeAnalysis;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Security;
using Cornerstone.Serialization;
using Microsoft.Maui.Storage;

#endregion

namespace Cornerstone.Platforms.Android;

[SourceReflection]
public partial class AndroidPlatformCredentialVault : PlatformCredentialVault
{
	#region Constructors

	[DependencyInjectionConstructor]
	public AndroidPlatformCredentialVault(IRuntimeInformation runtimeInformation)
		: base(runtimeInformation)
	{
	}

	#endregion

	#region Methods

	[UnconditionalSuppressMessage("Aot", "IL3050", Justification = "Base TryReadData cannot be annotated; JSON parse of vault payloads.")]
	[UnconditionalSuppressMessage("Trim", "IL2026", Justification = "Base TryReadData cannot be annotated; JSON parse of vault payloads.")]
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

	public override bool TryRemoveData(string name)
	{
		var keyName = GetVaultKey(name);
		return SecureStorage.Default.Remove(keyName);
	}

	[UnconditionalSuppressMessage("Aot", "IL3050", Justification = "Base TryWriteData cannot be annotated; JSON write of vault payloads.")]
	[UnconditionalSuppressMessage("Trim", "IL2026", Justification = "Base TryWriteData cannot be annotated; JSON write of vault payloads.")]
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