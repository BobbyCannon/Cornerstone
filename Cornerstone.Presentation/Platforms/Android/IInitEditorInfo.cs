using System;
using Android.Views.InputMethods;
using Cornerstone.Presentation.Android.Platform.SkiaPlatform;

namespace Cornerstone.Presentation.Android
{
    internal interface IInitEditorInfo
    {
        void InitEditorInfo(Func<TopLevelImpl, EditorInfo, IInputConnection> init);
    }
}
