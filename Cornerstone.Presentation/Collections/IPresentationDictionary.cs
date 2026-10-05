using System.Collections;
using System.Collections.Generic;

namespace Cornerstone.Presentation.Collections
{
    public interface IPresentationDictionary<TKey, TValue>
        : IDictionary<TKey, TValue>,
        IPresentationReadOnlyDictionary<TKey, TValue>,
        IDictionary
        where TKey : notnull
    {
    }
}
