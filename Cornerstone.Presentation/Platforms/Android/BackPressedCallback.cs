using System;
using System.Collections.Generic;
using System.Text;
using AndroidX.Activity;

namespace Cornerstone.Presentation.Android
{
    internal class BackPressedCallback(CornerstoneActivity activity) : OnBackPressedCallback(true)
    {
        public override void HandleOnBackPressed()
        {
            activity.OnBackInvoked();

            if (activity.ShouldNavigateBack)
            {
                this.Enabled = false;
                activity.OnBackPressedDispatcher?.OnBackPressed();
            }

            this.Enabled = true;
        }
    }
}
