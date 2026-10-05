using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Platforms.MacOS.Interop;
using Cornerstone.Presentation.Platform;
using MicroCom.Runtime;

namespace Cornerstone.Presentation.Platforms.MacOS
{
    internal sealed class CsnScreen(uint displayId)
        : PlatformScreen(new PlatformHandle(new IntPtr(displayId), "CGDirectDisplayID"))
    {
        public unsafe void Refresh(ICsnScreens native)
        {
            void* localizedName = null;
            var screen = native.GetScreen(displayId, &localizedName);

            IsPrimary = screen.IsPrimary.FromComBool();
            Scaling = screen.Scaling;
            Bounds = screen.Bounds.ToPixelRect();
            WorkingArea = screen.WorkingArea.ToPixelRect();
            CurrentOrientation = screen.Orientation switch
            {
                CsnScreenOrientation.UnknownOrientation => ScreenOrientation.None,
                CsnScreenOrientation.Landscape => ScreenOrientation.Landscape,
                CsnScreenOrientation.Portrait => ScreenOrientation.Portrait,
                CsnScreenOrientation.LandscapeFlipped => ScreenOrientation.LandscapeFlipped,
                CsnScreenOrientation.PortraitFlipped => ScreenOrientation.PortraitFlipped,
                _ => throw new ArgumentOutOfRangeException()
            };

            using var csnString = MicroComRuntime.CreateProxyOrNullFor<ICsnString>(localizedName, true);
            DisplayName = csnString?.String;
        }
    }

    internal class ScreenImpl : ScreensBase<uint, CsnScreen>, IDisposable
    {
        private ICsnScreens _native;

        public ScreenImpl(Func<ICsnScreenEvents, ICsnScreens> factory)
        {
            using var events = new CsnScreenEvents(this);
            _native = factory(events);
        }

        protected override unsafe int GetScreenCount() => _native.GetScreenIds(null);

        protected override unsafe IReadOnlyList<uint> GetAllScreenKeys()
        {
            var screenCount = _native.GetScreenIds(null);
            var displayIds = new uint[screenCount];
            fixed (uint* displayIdsPtr = displayIds)
            {
                _native.GetScreenIds(displayIdsPtr);
            }

            return displayIds;
        }

        protected override CsnScreen CreateScreenFromKey(uint key) => new(key);
        protected override void ScreenChanged(CsnScreen screen) => screen.Refresh(_native);

        protected override Screen? ScreenFromTopLevelCore(ITopLevelImpl topLevel)
        {
            var displayId = ((TopLevelImpl)topLevel).Native?.CurrentDisplayId;
            return displayId is not null && TryGetScreen(displayId.Value, out var screen) ? screen : null;
        }

        public void Dispose()
        {
            _native?.Dispose();
            _native = null!;
        }

        private class CsnScreenEvents(ScreenImpl screenImpl) : NativeCallbackBase, ICsnScreenEvents
        {
            public void OnChanged() => screenImpl.OnChanged();
        }
    }
}
