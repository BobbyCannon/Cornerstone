using System;
using System.Runtime.InteropServices;

namespace Cornerstone.Presentation.Controls.Utils;

internal static class ClipboardHelper
{
    public static bool IsExpectedClipboardException(Exception exception)
        => exception is TimeoutException
            or OperationCanceledException
            or UnauthorizedAccessException
            or COMException;
}
