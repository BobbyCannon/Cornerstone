using Cornerstone.Presentation.Rendering.Composition.Server;
using Cornerstone.Presentation.Rendering.Composition.Transport;

namespace Cornerstone.Presentation.Rendering.Composition;

internal interface ICompositorSerializable
{
    SimpleServerObject? TryGetServer(Compositor c);
    void SerializeChanges(Compositor c, BatchStreamWriter writer);
}