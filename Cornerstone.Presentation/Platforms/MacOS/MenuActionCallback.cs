using System;
using Cornerstone.Presentation.Platforms.MacOS.Interop;

namespace Cornerstone.Presentation.Platforms.MacOS
{
    internal class MenuActionCallback : NativeCallbackBase, ICsnActionCallback
    {
        private Action _action;

        public MenuActionCallback(Action action)
        {
            _action = action;
        }

        void ICsnActionCallback.Run()
        {
            _action?.Invoke();
        }
    }
}
