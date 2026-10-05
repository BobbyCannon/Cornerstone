// Copyright (c) The Cornerstone Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Cornerstone.Presentation.Controls.Platform;

namespace Cornerstone.Presentation.Platforms.Windows
{
    internal partial class WindowImpl
    {
        protected virtual unsafe IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            IntPtr lRet = IntPtr.Zero;
            bool callDwp = true;

            if (_isClientAreaExtended)
            {
                lRet = CustomCaptionProc(hWnd, msg, wParam, lParam, ref callDwp);
            }

            if (callDwp)
            {
                lRet = AppWndProc(hWnd, msg, wParam, lParam);
            }

            return lRet;
        }
        
        public INativeControlHostImpl NativeControlHost => _nativeControlHost;

        protected virtual bool ShouldTakeFocusOnClick => true;
    }
}
