using System;
using static Cornerstone.Presentation.Platforms.Windows.Interop.UnmanagedMethods;

namespace Cornerstone.Presentation.Platforms.Windows.Input
{
    internal struct Imm32CaretManager
    {
        private bool _isCaretCreated;

        public void TryCreate(IntPtr hwnd)
        {
            if (!_isCaretCreated)
            {
                _isCaretCreated = CreateCaret(hwnd, IntPtr.Zero, 2, 2);               
            }
        }

        public void TryMove(int x, int y)
        {
            if (_isCaretCreated)
            {
                SetCaretPos(x, y);
            }
        }

        public void TryDestroy()
        {
            if (_isCaretCreated)
            {
                DestroyCaret();

                _isCaretCreated = false;
            }
        }
    }
}
