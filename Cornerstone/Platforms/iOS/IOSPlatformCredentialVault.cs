#region References

using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Security;

#endregion

namespace Cornerstone.Platforms.iOS;

[SourceReflection]
public partial class IOSPlatformCredentialVault : PlatformCredentialVault
{
	#region Constructors

	[DependencyInjectionConstructor]
	public IOSPlatformCredentialVault(IRuntimeInformation runtimeInformation)
		: base(runtimeInformation)
	{
	}

	#endregion

	#region Methods

	public override bool TryReadData<T>(string name, out T data)
	{
		data = default;
		return false;
	}

	public override bool TryRemoveData(string name)
	{
		return false;
	}

	public override bool TryWriteData<T>(string name, T data)
	{
		return false;
	}

	#endregion
}