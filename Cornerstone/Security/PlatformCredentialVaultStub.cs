#region References

using System.Collections.Generic;
using Cornerstone.Extensions;
using Cornerstone.Runtime;
using Cornerstone.Serialization;

#endregion

namespace Cornerstone.Security;

public class PlatformCredentialVaultStub : PlatformCredentialVault
{
	#region Fields

	private readonly Dictionary<string, string> _dictionary;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public PlatformCredentialVaultStub(IRuntimeInformation runtimeInformation)
		: base(runtimeInformation)
	{
		_dictionary = new Dictionary<string, string>();
	}

	#endregion

	#region Methods

	
	public override bool TryReadData<T>(string name, out T data)
	{
		if (_dictionary.TryGetValue(name, out var value))
		{
			data = value.FromJson<T>();
			return true;
		}

		data = default;
		return false;
	}

	
	public override bool TryRemoveData(string name)
	{
		return _dictionary.Remove(name);
	}

	
	public override bool TryWriteData<T>(string name, T data)
	{
		_dictionary.AddOrUpdate(name, data.ToRawJson());
		return true;
	}

	#endregion
}