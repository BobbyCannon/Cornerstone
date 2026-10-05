using System;
using System.Collections.ObjectModel;

using Cornerstone.Presentation.Controls.Platform;

namespace Cornerstone.Presentation.FreeDesktop
{
    internal class LinuxMountedVolumeInfoProvider : IMountedVolumeInfoProvider
    {
        public IDisposable Listen(ObservableCollection<MountedVolumeInfo> mountedDrives)
        {
            return new LinuxMountedVolumeInfoListener(ref mountedDrives);
        }
    }
}
