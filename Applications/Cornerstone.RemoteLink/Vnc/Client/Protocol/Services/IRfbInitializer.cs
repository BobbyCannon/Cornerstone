using System.Threading;
using System.Threading.Tasks;

namespace Cornerstone.RemoteLink.Vnc.Client.Protocol.Services
{
    /// <summary>
    /// Provides methods for doing a RFB compliant initialization.
    /// </summary>
    public interface IRfbInitializer
    {
        /// <summary>
        /// Executes a initialization.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task InitializeAsync(CancellationToken cancellationToken = default);
    }
}
