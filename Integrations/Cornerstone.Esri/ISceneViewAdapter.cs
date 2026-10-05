#region References

using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.NativeHosts;

#endregion

namespace Cornerstone.Esri;

public interface ISceneViewAdapter : ISceneView, IPausableNativeSurface
{
	#region Methods

	IPlatformHandle AttachToHost(IPlatformHandle parent, InputElement inputHost)
	{
		return PlatformHandle;
	}

	void DetachFromHost()
	{
	}

	#endregion
}
