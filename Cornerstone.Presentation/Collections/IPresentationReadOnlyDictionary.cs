using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Cornerstone.Presentation.Collections
{
    public interface IPresentationReadOnlyDictionary<TKey, TValue>
        : IReadOnlyDictionary<TKey, TValue>,
        INotifyCollectionChanged,
        INotifyPropertyChanged
        where TKey : notnull
    {
    }
}
