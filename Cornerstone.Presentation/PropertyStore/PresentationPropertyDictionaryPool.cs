using System.Collections.Generic;
using Cornerstone.Presentation.Utilities;

namespace Cornerstone.Presentation.PropertyStore
{
    internal static class PresentationPropertyDictionaryPool<TValue>
    {
        private const int MaxPoolSize = 4;
        private static readonly Stack<PresentationPropertyDictionary<TValue>> _pool = new();

        public static PresentationPropertyDictionary<TValue> Get()
        {
            return _pool.Count == 0 ? new() : _pool.Pop();
        }

        public static void Release(PresentationPropertyDictionary<TValue> dictionary)
        {
            if (_pool.Count < MaxPoolSize)
            {
                dictionary.Clear();
                _pool.Push(dictionary);
            }
        }
    }
}
