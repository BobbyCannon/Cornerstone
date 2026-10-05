using System;
using Android.Views;
using Android.Views.InputMethods;
using Cornerstone.Presentation.Android.Platform.SkiaPlatform;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Platform;

namespace Cornerstone.Presentation.Android
{
    public partial class CornerstoneView : IInitEditorInfo
    {
        private Func<TopLevelImpl, EditorInfo, IInputConnection>? _initEditorInfo;

        public override IInputConnection OnCreateInputConnection(EditorInfo? outAttrs)
        {
            return _initEditorInfo?.Invoke(_view, outAttrs!)!;
        }

        void IInitEditorInfo.InitEditorInfo(Func<TopLevelImpl, EditorInfo, IInputConnection> init)
        {
            _initEditorInfo = init;
        }

        protected override void OnFocusChanged(bool gainFocus, FocusSearchDirection direction, global::Android.Graphics.Rect? previouslyFocusedRect)
        {
            base.OnFocusChanged(gainFocus, direction, previouslyFocusedRect);
            _accessHelper?.OnFocusChanged(gainFocus, (int)direction, previouslyFocusedRect);
        }

        protected override bool DispatchHoverEvent(MotionEvent? e)
        {
            var res = _view.PointerHelper.DispatchMotionEvent(e, out var callBase);
            callBase = (_accessHelper?.DispatchHoverEvent(e!) == true) && callBase;

            var baseResult = callBase && base.DispatchHoverEvent(e);

            return res ?? baseResult;
        }

        protected override bool DispatchGenericPointerEvent(MotionEvent? e)
        {
            var result = _view.PointerHelper.DispatchMotionEvent(e, out var callBase);

            var baseResult = callBase && base.DispatchGenericPointerEvent(e);

            return result ?? baseResult;
        }

        public override bool DispatchTouchEvent(MotionEvent? e)
        {
            if (NativeAirspace.BehindComposition)
            {
                if (ShouldPassTouchToNative(e))
                    return base.DispatchTouchEvent(e);

                var handled = _view.PointerHelper.DispatchMotionEvent(e, out _);
                if (handled == true)
                    RequestFocus();
                return handled ?? false;
            }

            var result = _view.PointerHelper.DispatchMotionEvent(e, out var callBase);
            var baseResult = callBase && base.DispatchTouchEvent(e);

            if (result == true)
            {
                // Request focus for this view
                RequestFocus();
            }

            return result ?? baseResult;
        }

        private bool ShouldPassTouchToNative(MotionEvent? e)
        {
            if (!NativeAirspace.BehindComposition || e is null || _view.InputRoot is not { } root)
                return false;

            var point = new Point(e.GetX(), e.GetY()) / _view.RenderScaling;
            return NativeAirspace.IsHoleHit(root.RootElement.InputHitTest(point));
        }

        public override bool DispatchKeyEvent(KeyEvent? e)
        {
            var res = _view.KeyboardHelper.DispatchKeyEvent(e, out var callBase);
            if (res == false)
                callBase = _accessHelper?.DispatchKeyEvent(e!) == false && callBase;

            var baseResult = callBase && base.DispatchKeyEvent(e);

            return res ?? baseResult;
        }
    }
}
