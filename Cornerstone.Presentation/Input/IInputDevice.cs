using Cornerstone.Presentation.Input.Raw;
using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Input
{
    [NotClientImplementable, PrivateApi]
    public interface IInputDevice
    {
        /// <summary>
        /// Processes raw event. Is called after preprocessing by InputManager
        /// </summary>
        /// <param name="ev"></param>
        void ProcessRawEvent(RawInputEventArgs ev);
    }
}
