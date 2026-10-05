using System;
using AudioToolbox;
using Cornerstone.Presentation.Controls;
using UIKit;
using Cornerstone.Presentation.Controls.Feedback;

namespace Cornerstone.Presentation.iOS
{
    internal class IOSPlatformFeedback(CornerstoneView cornerstoneView) : IPlatformFeedback
    {
        private static SystemSound s_defaultSound = new(1104);
        public bool Perform(FeedbackAction feedback, FeedbackType type)
        {
            var performedFeedback = false;
            var playSound = type is FeedbackType.Sound or FeedbackType.Auto;
            var vibrate = type is FeedbackType.Haptic or FeedbackType.Auto;

            if (feedback == FeedbackAction.Click && playSound)
            {
                s_defaultSound.PlaySystemSound();
                performedFeedback = true;
            }

#if !TVOS
            if (vibrate)
            {
                if (FeedbackToImpactStyle(feedback) is { } uIImpactFeedbackStyle)
                {
                    using var generator = OperatingSystem.IsIOSVersionAtLeast(17, 5) || OperatingSystem.IsMacCatalystVersionAtLeast(17, 5)
                        ? UIImpactFeedbackGenerator.GetFeedbackGenerator(uIImpactFeedbackStyle, cornerstoneView)
                        : new UIImpactFeedbackGenerator(uIImpactFeedbackStyle);
                    generator.ImpactOccurred();
                    performedFeedback = true;
                }
            }

            UIImpactFeedbackStyle? FeedbackToImpactStyle(FeedbackAction feedback)
            {
                if (feedback == FeedbackAction.Click)
                {
                    return UIImpactFeedbackStyle.Light;
                }
                else if (feedback == FeedbackAction.Hold)
                {
                    return UIImpactFeedbackStyle.Medium;
                }

                return null;
            }
#else
            _ = cornerstoneView;
#endif

            return performedFeedback;
        }
    }
}
