#region References

using Cornerstone.RemoteLink.Keystone.State;
using Cornerstone.Keystone;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.RemoteLink.Keystone;

[SourceReflection]
[DependencyInjected]
public partial class AppState : KeystoneState
{
	#region Constructors

	[DependencyInjectionConstructor]
	public AppState(AppSettings settings)
	{
		Settings = Track(settings);
	}

	#endregion

	#region Properties

	public AppSettings Settings { get; }

	#endregion
}