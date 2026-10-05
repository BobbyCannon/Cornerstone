using Cornerstone.Presentation.Input;
using static Cornerstone.Presentation.Platforms.Windows.Interop.UnmanagedMethods;
using static Cornerstone.Presentation.Platforms.Windows.Interop.UnmanagedMethods.VirtualKeyStates;

namespace Cornerstone.Presentation.Platforms.Windows.Input
{
    internal sealed class WindowsKeyboardDevice : KeyboardDevice
    {
        public new static WindowsKeyboardDevice Instance { get; } = new();

        public unsafe RawInputModifiers Modifiers
        {
            get
            {
                fixed (byte* keyStates = stackalloc byte[256])
                {
                    GetKeyboardState(keyStates);

                    var result = RawInputModifiers.None;

                    if (((keyStates[(int)VK_LMENU] | keyStates[(int)VK_RMENU]) & 0x80) != 0)
                    {
                        result |= RawInputModifiers.Alt;
                    }

                    if (((keyStates[(int)VK_LCONTROL] | keyStates[(int)VK_RCONTROL]) & 0x80) != 0)
                    {
                        result |= RawInputModifiers.Control;
                    }

                    if (((keyStates[(int)VK_LSHIFT] | keyStates[(int)VK_RSHIFT]) & 0x80) != 0)
                    {
                        result |= RawInputModifiers.Shift;
                    }

                    if (((keyStates[(int)VK_LWIN] | keyStates[(int)VK_RWIN]) & 0x80) != 0)
                    {
                        result |= RawInputModifiers.Meta;
                    }

                    return result;
                }
            }
        }

        private WindowsKeyboardDevice()
        {
        }
    }
}
