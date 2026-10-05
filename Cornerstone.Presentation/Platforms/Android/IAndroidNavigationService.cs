using System;

namespace Cornerstone.Presentation.Android
{
    public interface IActivityNavigationService
    {
        event EventHandler<AndroidBackRequestedEventArgs> BackRequested;
    }

    public class AndroidBackRequestedEventArgs : EventArgs
    {
        public bool Handled { get; set; }
    }
}
