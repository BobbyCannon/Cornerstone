using System;
using Cornerstone.Presentation.Platforms.MacOS.Interop;

namespace Cornerstone.Presentation.Platforms.MacOS
{
    internal class PredicateCallback : NativeCallbackBase, ICsnPredicateCallback
    {
        private Func<bool> _predicate;

        public PredicateCallback(Func<bool> predicate)
        {
            _predicate = predicate;
        }

        int ICsnPredicateCallback.Evaluate()
        {
            return _predicate().AsComBool();
        }
    }
}
