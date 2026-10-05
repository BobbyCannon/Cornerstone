using System.Threading.Tasks;
using Cornerstone.Presentation.Platforms.MacOS.Interop;
using Cornerstone.Presentation.Platform;

namespace Cornerstone.Presentation.Platforms.MacOS
{
    internal class PlatformBehaviorInhibition : IPlatformBehaviorInhibition
    {
        readonly ICsnPlatformBehaviorInhibition _native;

        internal PlatformBehaviorInhibition(ICsnPlatformBehaviorInhibition native)
            => _native = native;

        public Task SetInhibitAppSleep(bool inhibitAppSleep, string reason)
        {
            _native.SetInhibitAppSleep(inhibitAppSleep ? 1 : 0, reason);
            return Task.CompletedTask;
        }
    }
}
