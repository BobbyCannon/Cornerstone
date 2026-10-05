using System.Threading.Tasks;

namespace Cornerstone.Presentation.Platform
{
    /// <summary>
    /// Allows to inhibit platform specific behavior.
    /// </summary>
    public interface IPlatformBehaviorInhibition
    {
        Task SetInhibitAppSleep(bool inhibitAppSleep, string reason);
    }
}
