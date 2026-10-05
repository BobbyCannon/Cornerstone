using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Cornerstone.Presentation.Threading;

namespace Cornerstone.Presentation.Reactive;

internal class LightweightSubject<T> : LightweightObservableBase<T>, IPresentationSubject<T>
{
    public void OnCompleted()
    {
        PublishCompleted();
    }

    public void OnError(Exception error)
    {
        PublishError(error);
    }

    public void OnNext(T value)
    {
        PublishNext(value);
    }

    protected override void Initialize() { }

    protected override void Deinitialize() { }
}
