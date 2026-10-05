using System.Threading.Tasks;
using Cornerstone.RemoteLink.Vnc.Client.Utils;

namespace Cornerstone.RemoteLink.Vnc.Client.Protocol.Services
{
    /// <summary>
    /// Describes a background thread that receives and processes RFB protocol messages.
    /// </summary>
    public interface IRfbMessageReceiver : IBackgroundThread
    {
        /// <summary>
        /// Starts the receive loop.
        /// </summary>
        void StartReceiveLoop();

        /// <summary>
        /// Stops the receive loop and waits for completion.
        /// </summary>
        Task StopReceiveLoopAsync();
    }
}
