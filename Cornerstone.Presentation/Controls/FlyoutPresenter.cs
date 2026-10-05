using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.Controls.Elements;

namespace Cornerstone.Presentation.Controls
{
    public class FlyoutPresenter : ContentControl
    {
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                var host = this.FindLogicalAncestorOfType<Popup>();
                if (host != null)
                {
                    host.IsOpen = false;
                    e.Handled = true;
                }
            }

            base.OnKeyDown(e);
        }
    }
}
