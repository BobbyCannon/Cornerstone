#region References

using System.Collections.Generic;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Security;

public class CredentialVaultStub : CredentialVault
{
	#region Fields

	private readonly Dictionary<string, string> _dictionary;

	#endregion

	#region Constructors

	/// <inheritdoc />
	public CredentialVaultStub(IRuntimeInformation runtimeInformation, IDispatcher dispatcher)
		: base(runtimeInformation, dispatcher)
	{
		_dictionary = new Dictionary<string, string>();
	}

	#endregion

	#region Methods

	/// <inheritdoc />
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

	/// <inheritdoc />
	public override bool TryRemoveData(string name)
	{
		return _dictionary.Remove(name);
	}

	/// <inheritdoc />
	public override bool TryWriteData<T>(string name, T data)
	{
		_dictionary.AddOrUpdate(name, data.ToRawJson());
		return true;
	}

	#endregion
}