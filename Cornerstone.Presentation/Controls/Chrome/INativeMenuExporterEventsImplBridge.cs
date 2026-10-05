using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Controls.Chrome
{
    [PrivateApi]
    public interface INativeMenuExporterEventsImplBridge
    {
        void RaiseNeedsUpdate ();
        void RaiseOpening();
        void RaiseClosed();
    }
}
