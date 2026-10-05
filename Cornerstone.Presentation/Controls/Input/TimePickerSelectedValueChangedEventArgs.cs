using System;
using Cornerstone.Presentation.Controls;

namespace Cornerstone.Presentation.Controls.Input
{
    public class TimePickerSelectedValueChangedEventArgs
    {
        public TimeSpan? OldTime { get; }
        public TimeSpan? NewTime { get; }
        public TimePickerSelectedValueChangedEventArgs(TimeSpan? old, TimeSpan? newT)
        {
            OldTime = old;
            NewTime = newT;
        }
    }
}
