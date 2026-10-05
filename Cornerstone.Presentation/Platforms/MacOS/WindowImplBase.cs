using System;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Platforms.MacOS.Interop;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Controls.Chrome;

namespace Cornerstone.Presentation.Platforms.MacOS
{
    internal abstract class WindowBaseImpl : TopLevelImpl, IWindowBaseImpl
    {
        internal WindowBaseImpl(ICornerstoneNativeFactory factory) : base(factory)
        {

        }

        public new ICsnWindowBase? Native => _handle?.Native as ICsnWindowBase;

        public PixelPoint Position
        {
            get => Native?.Position.ToPixelPoint() ?? default;
            set => Native?.SetPosition(value.ToCsnPoint());
        }

        public Action? Deactivated { get; set; }
        public Action? Activated { get; set; }

        public Action<PixelPoint>? PositionChanged { get; set; }

        public Size? FrameSize
        {
            get
            {
                if (Native != null)
                {
                    unsafe
                    {
                        var s = new CsnSize { Width = -1, Height = -1 };
                        Native.GetFrameSize(&s);
                        return s.Width < 0  && s.Height < 0 ? null : new Size(s.Width, s.Height);
                    }
                }

                return default;
            }
        }
        
        internal override void Init(MacOSTopLevelHandle handle)
        {
            _handle = handle;

            base.Init(handle);

            int defaultWidth = 0, defaultHeight = 0;

            var monitor = this.TryGetFeature<IScreenImpl>()!.AllScreens
                .OrderBy(x => x.Scaling)
                .FirstOrDefault(m => m.Bounds.Contains(Position));

            if (monitor != null)
            {
                // Emulate Windows 7+ default window size behavior.
                defaultWidth = (int)(monitor.WorkingArea.Width * 0.75d);
                defaultHeight = (int)(monitor.WorkingArea.Height * 0.7d);
            }

            defaultWidth = Math.Max(defaultWidth, 300);
            defaultHeight = Math.Max(defaultHeight, 200);

            Resize(new Size(defaultWidth, defaultHeight), WindowResizeReason.Layout);
        }

        public void Activate()
        {
            Native?.Activate();
        }

        public void Resize(Size clientSize, WindowResizeReason reason)
        {
            Native?.Resize(clientSize.Width, clientSize.Height, (CsnPlatformResizeReason)reason);
        }
        
        public override void SetFrameThemeVariant(PlatformThemeVariant? themeVariant)
        {
            var settings = PresentationLocator.Current.GetService<IPlatformSettings>();
            themeVariant ??= settings?.GetColorValues().ThemeVariant ?? PlatformThemeVariant.Light;
            Native?.SetFrameThemeVariant((CsnPlatformThemeVariant)themeVariant);
        }

        public override void Dispose()
        {
            Native?.Close();
            base.Dispose();
        }

        public virtual void Show(bool activate, bool isDialog)
        {
            Native?.Show(activate.AsComBool(), isDialog.AsComBool());
        }

        public void Hide()
        {
            Native?.Hide();
        }

        public void BeginMoveDrag(PointerPressedEventArgs e)
        {
            Native?.BeginMoveDrag();
        }

        public Size MaxAutoSizeHint => this.TryGetFeature<IScreenImpl>()!.AllScreens
            .Select(s => s.Bounds.Size.ToSize(1))
            .OrderByDescending(x => x.Width + x.Height).FirstOrDefault();

        public void SetTopmost(bool value)
        {
            Native?.SetTopMost(value.AsComBool());
        }

        // TODO
        public void BeginResizeDrag(WindowEdge edge, PointerPressedEventArgs e)
        {

        }

        public void SetMinMaxSize(Size minSize, Size maxSize)
        {
            Native?.SetMinMaxSize(minSize.ToCsnSize(), maxSize.ToCsnSize());
        }

        protected class WindowBaseEvents : TopLevelEvents, ICsnWindowBaseEvents
        {
            private readonly WindowBaseImpl _parent;

            public WindowBaseEvents(WindowBaseImpl parent) : base(parent)
            {
                _parent = parent;
            }

            void ICsnWindowBaseEvents.PositionChanged(CsnPoint position)
            {
                _parent.PositionChanged?.Invoke(position.ToPixelPoint());
            }

            void ICsnWindowBaseEvents.Activated() => _parent.Activated?.Invoke();

            void ICsnWindowBaseEvents.Deactivated() => _parent.Deactivated?.Invoke();
        }
    }
}
