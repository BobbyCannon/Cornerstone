using Cornerstone.RemoteLink.Vnc.Client.Utils;

namespace Cornerstone.RemoteLink.Vnc.Client
{
    /// <summary>
    /// Represents parameters for establishing a RFB transport.
    /// </summary>
    public abstract class TransportParameters : FreezableParametersObject
    {
        /// <inheritdoc />
        public abstract override string ToString();
    }
}
