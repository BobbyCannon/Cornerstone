#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.NativeHosts;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Platform;

#endregion

namespace Cornerstone.Vlc;

public interface IVideoViewAdapter : IVideoView, INativeSurface
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
