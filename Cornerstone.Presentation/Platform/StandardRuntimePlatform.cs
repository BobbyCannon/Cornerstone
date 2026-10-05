using System;
using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Platform
{
    [PrivateApi]
    public class StandardRuntimePlatform : IRuntimePlatform
    {
        public virtual RuntimePlatformInfo GetRuntimeInfo() => new()
        {
            IsDesktop = OperatingSystem.IsWindows()
                        || OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst()
                        || OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD(),
            IsMobile = OperatingSystem.IsAndroid() || (OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst()),
            IsTV = OperatingSystem.IsTvOS()
        };
    }
}
