using System;

namespace Cornerstone.Presentation.Reactive;

internal interface IPresentationSubject<T> : IObserver<T>, IObservable<T> /*, ISubject<T> */
{
    
}
