using System;
using Cornerstone.Presentation.Rendering.Composition.Transport;

namespace Cornerstone.Presentation.Rendering.Composition.Server;

class SimpleServerObject
{
    public ServerCompositor Compositor { get; }

    public SimpleServerObject(ServerCompositor compositor)
    {
        Compositor = compositor;
    }

    protected virtual void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
    {

    }

    public void DeserializeChanges(BatchStreamReader reader, CompositionBatch batch)
    {
        DeserializeChangesCore(reader, batch.CommittedAt);
        ValuesInvalidated();
    }

    protected virtual void ValuesInvalidated()
    {

    }
    
    protected void SetValue<T>(CompositionProperty prop, ref T field, T value) => field = value;

    protected T GetValue<T>(CompositionProperty prop, ref T field) => field;
}