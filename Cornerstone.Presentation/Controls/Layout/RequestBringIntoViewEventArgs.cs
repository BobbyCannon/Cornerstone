using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.VisualTree;

namespace Cornerstone.Presentation.Controls.Layout
{
    public class RequestBringIntoViewEventArgs : RoutedEventArgs
    {
        public Visual? TargetObject { get; set; }

        public Rect TargetRect { get; set; }
    }
}
