using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;

namespace Cornerstone.Presentation.Dialogs.Internal
{
    public class ChildFitter : Decorator
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(0, 0);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Child?.Measure(finalSize);
            base.ArrangeOverride(finalSize);
            return finalSize;
        }
    }
}
