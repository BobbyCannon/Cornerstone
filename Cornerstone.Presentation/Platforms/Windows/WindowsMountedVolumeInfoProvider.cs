using System;
using System.Collections.ObjectModel;
using Cornerstone.Presentation.Controls.Platform;

namespace Cornerstone.Presentation.Platforms.Windows
{
    internal class WindowsMountedVolumeInfoProvider : IMountedVolumeInfoProvider
    {
        public IDisposable Listen(ObservableCollection<MountedVolumeInfo> mountedDrives)
        {
            return new WindowsMountedVolumeInfoListener(mountedDrives);
        }
    }
}
