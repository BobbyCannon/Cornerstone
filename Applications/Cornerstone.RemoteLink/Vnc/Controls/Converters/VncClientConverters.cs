using Cornerstone.Presentation.Data.Converters;
using Cornerstone.RemoteLink.Vnc.Client.Protocol;

namespace Cornerstone.RemoteLink.Vnc.Controls.Converters
{
    /// <summary>
    /// Provides a set of useful <see cref="IValueConverter"/>s for working with the vnc client library.
    /// </summary>
    public static class VncClientConverters
    {
        /// <summary>
        /// A value converter that returns a readable string for a given <see cref="RfbProtocolVersion"/>.
        /// </summary>
        public static readonly IValueConverter RfbProtocolVersionToString = new FuncValueConverter<RfbProtocolVersion, string>(v => v.ToReadableString());
    }
}
