using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Controls.Automation.Peers;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Rendering;
using Cornerstone.Presentation.Rendering.SceneGraph;
using Cornerstone.Presentation.Backends.Skia;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.VisualTree;
using SkiaSharp;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Chrome;

namespace Cornerstone.Presentation.Controls
{
    public class NativeControlHost : Control
    {
        private PresentationSource? _currentRoot;
        private INativeControlHostImpl? _currentHost;
        private INativeControlHostControlTopLevelAttachment? _attachment;
        private IPlatformHandle? _nativeControlHandle;
        private object _airspaceOwner;
        private bool _queuedForDestruction;
        private bool _queuedForMoveResize;
        private readonly List<Visual> _propertyChangedSubscriptions = new();

        static NativeControlHost()
        {
            FlowDirectionProperty.Changed.AddClassHandler<NativeControlHost>(OnFlowDirectionChanged);
        }

        internal IPlatformHandle? NativeControlHandle
        {
            get => _nativeControlHandle;
            set
            {
                if (_nativeControlHandle != value)
                {
                    _nativeControlHandle = value;
                    NativeControlHandleChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        internal event EventHandler? NativeControlHandleChanged;

        /// <inheritdoc />
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _currentRoot = (PresentationSource)e.PresentationSource;
            if (NativeAirspace.BehindComposition)
            {
                _airspaceOwner = _currentRoot.PlatformImpl;
                if (_airspaceOwner != null)
                    NativeAirspace.AddAttachedHost(_airspaceOwner);
            }

            var visual = (Visual)this;
            while (visual != null)
            {
                visual.PropertyChanged += PropertyChangedHandler;
                _propertyChangedSubscriptions.Add(visual);

                visual = visual.GetVisualParent();
            }

            UpdateHost();
        }
        
        private static void OnFlowDirectionChanged(NativeControlHost nativeControlHost,
            PresentationPropertyChangedEventArgs propertyChangedEventArgs)
        {
            nativeControlHost.TryUpdateNativeControlPosition();
        }

        private void PropertyChangedHandler(object? sender, PresentationPropertyChangedEventArgs e)
        {
            if (e.IsEffectiveValueChange &&
                (e.Property == BoundsProperty ||
                 e.Property == IsVisibleProperty ||
                 e.Property == ClipProperty ||
                 e.Property == ClipToBoundsProperty ||
                 e.Property == Border.CornerRadiusProperty))
                EnqueueForMoveResize();
        }

        /// <inheritdoc />
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            RemoveAirspaceHole();
            if (_airspaceOwner != null)
            {
                SyncBackdropHoles();
                NativeAirspace.RemoveAttachedHost(_airspaceOwner);
                _airspaceOwner = null;
            }

            _currentRoot = null;
            foreach (var v in _propertyChangedSubscriptions)
                v.PropertyChanged -= PropertyChangedHandler;
            _propertyChangedSubscriptions.Clear();
            UpdateHost();
        }


        private void UpdateHost()
        {
            _queuedForMoveResize = false;
            _currentHost = _currentRoot?.PlatformImpl?.TryGetFeature<INativeControlHostImpl>();
            
            if (_currentHost != null)
            {
                // If there is an existing attachment, ensure that we are attached to the proper host or destroy the attachment
                if (_attachment != null && _attachment.AttachedTo != _currentHost)
                {
                    if (_attachment != null)
                    {
                        if (_attachment.IsCompatibleWith(_currentHost))
                        {
                            _attachment.AttachedTo = _currentHost;
                        }
                        else
                        {
                            _attachment.Dispose();
                            _attachment = null;
                        }
                    }
                }

                // If there is no attachment, but the control exists,
                // attempt to attach to the current toplevel or destroy the control if it's incompatible
                if (_attachment == null && NativeControlHandle != null)
                {
                    if (_currentHost.IsCompatibleWith(NativeControlHandle))
                        _attachment = _currentHost.CreateNewAttachment(NativeControlHandle);
                    else
                        DestroyNativeControl();
                }

                // There is no control handle an no attachment, create both
                if (NativeControlHandle == null)
                {
                    _attachment = _currentHost.CreateNewAttachment(parent =>
                        NativeControlHandle = CreateNativeControlCore(parent));
                }
            }
            else
            {
                // Immediately detach the control from the current toplevel if there is an existing attachment
                if (_attachment != null)
                    _attachment.AttachedTo = null;
                
                // Don't destroy the control immediately, it might be just being reparented to another TopLevel
                if (NativeControlHandle != null && !_queuedForDestruction)
                {
                    _queuedForDestruction = true;
                    Dispatcher.UIThread.Post(CheckDestruction, DispatcherPriority.Background);
                }
            }

            if (_attachment?.AttachedTo != _currentHost)
                return;

            TryUpdateNativeControlPosition();
        }

        
        internal Rect? GetAbsoluteBounds()
        {
            Debug.Assert(_currentRoot is not null);
            var root = _currentRoot.RootVisual;
            if (root == null)
                return null;
            return NativeAirspaceClip.GetClippedAbsoluteBounds(this, root);
        }

        private void EnqueueForMoveResize()
        {
            if(_queuedForMoveResize)
                return;
            _queuedForMoveResize = true;
            Dispatcher.UIThread.Post(UpdateHost, DispatcherPriority.AfterRender);
        }

        /// <summary>
        /// Attaches <paramref name="handle"/> as the platform child.
        /// The first attach often creates Cornerstone's default child before the adapter
        /// publishes a real surface. Visibility changes only show or hide that child;
        /// they do not replace it.
        /// </summary>
        internal void HostPlatformHandle(IPlatformHandle handle)
        {
            if (handle == null || _currentRoot == null)
                return;

            if (!ReferenceEquals(NativeControlHandle, handle) && NativeControlHandle != null)
                DestroyNativeControl();

            UpdateHost();
        }

        public bool TryUpdateNativeControlPosition()
        {
            if (_currentRoot == null)
            {
                RemoveAirspaceHole();
                return false;
            }

            var bounds = GetAbsoluteBounds();
            var show = IsEffectivelyVisible && bounds.HasValue && !bounds.Value.IsEmpty() &&
                       bounds.Value.Width > 0 && bounds.Value.Height > 0;

            if (show)
            {
                if (_currentHost != null)
                    _attachment?.ShowInBounds(bounds.Value);
                PublishAirspaceHole(bounds.Value);
            }
            else
            {
                if (_currentHost != null)
                    _attachment?.HideWithSize(Bounds.Size);
                RemoveAirspaceHole();
            }

            return _currentHost != null;
        }

        private void PublishAirspaceHole(Rect bounds)
        {
            if (_currentRoot == null)
                return;

            var scaling = _currentRoot.RenderScaling;
            var treeOrder = NativeAirspaceClip.GetTreeOrder(this, _currentRoot.RootVisual);
            var cornerRadius = NativeAirspaceClip.GetHoleCornerRadius(this, _currentRoot.RootVisual);
            var hole = new NativeAirspaceHole(bounds, scaling, treeOrder, treeOrder, CompositionVisual?.Server,
                cornerRadius);
            _currentRoot.Renderer.CompositionTarget.PublishNativeAirspaceHole(this, hole);
            SyncBackdropHoles();
            InvalidateVisual();
        }

        private void RemoveAirspaceHole()
        {
            _currentRoot?.Renderer.CompositionTarget.RemoveNativeAirspaceHole(this);
            SyncBackdropHoles();
            InvalidateVisual();
        }

        private void SyncBackdropHoles()
        {
            if (_airspaceOwner == null || _currentRoot == null)
                return;
            NativeAirspace.SetHoles(_airspaceOwner, _currentRoot.Renderer.CompositionTarget.NativeAirspaceHoles);
            (_airspaceOwner as INativeAirspaceInputHost)?.SyncNativeAirspaceInput();
        }

        private void CheckDestruction()
        {
            _queuedForDestruction = false;
            if (_currentRoot == null)
                DestroyNativeControl();
        }
        
        protected virtual IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            if (_currentHost == null)
                throw new InvalidOperationException();
            return _currentHost.CreateDefaultChild(parent);
        }

        private void DestroyNativeControl()
        {
            if (NativeControlHandle != null)
            {
                _attachment?.Dispose();
                _attachment = null;
                
                DestroyNativeControlCore(NativeControlHandle);
                NativeControlHandle = null;
            }
        }

        protected virtual void DestroyNativeControlCore(IPlatformHandle control)
        {
            if (control is INativeControlHostDestroyableControlHandle nativeControlHostDestroyableControlHandle)
            {
                nativeControlHostDestroyableControlHandle.Destroy();
            }
        }

        public override void Render(DrawingContext context)
        {
            if (!NativeAirspace.BehindComposition)
                return;
            var radii = default(CornerRadius);
            if (_currentRoot?.RootVisual != null)
                radii = NativeAirspaceClip.GetHoleCornerRadius(this, _currentRoot.RootVisual);
            context.Custom(new NativeAirspaceHoleDrawOperation(new Rect(Bounds.Size), radii));
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new NativeControlHostPeer(this);

        private readonly struct NativeAirspaceHoleDrawOperation : ICustomDrawOperation
        {
            public NativeAirspaceHoleDrawOperation(Rect bounds, CornerRadius cornerRadius)
            {
                Bounds = bounds;
                CornerRadius = cornerRadius;
            }

            public Rect Bounds { get; }

            public CornerRadius CornerRadius { get; }

            public void Dispose()
            {
            }

            public bool Equals(ICustomDrawOperation other) =>
                other is NativeAirspaceHoleDrawOperation hole && hole.Bounds == Bounds
                && hole.CornerRadius == CornerRadius;

            public bool HitTest(Point p) => new RoundedRect(Bounds, CornerRadius).ContainsExclusive(p);

            public void Render(ImmediateDrawingContext context)
            {
                var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
                if (leaseFeature == null)
                    return;

                using var lease = leaseFeature.Lease();
                using var paint = new SKPaint
                {
                    Color = SKColors.Transparent,
                    BlendMode = SKBlendMode.Src
                };
                var rect = Bounds.ToSKRect();
                if (CornerRadius == default)
                    lease.SkCanvas.DrawRect(rect, paint);
                else
                {
                    using var round = new SKRoundRect(rect);
                    round.SetRectRadii(rect, new[]
                    {
                        new SKPoint((float)CornerRadius.TopLeft, (float)CornerRadius.TopLeft),
                        new SKPoint((float)CornerRadius.TopRight, (float)CornerRadius.TopRight),
                        new SKPoint((float)CornerRadius.BottomRight, (float)CornerRadius.BottomRight),
                        new SKPoint((float)CornerRadius.BottomLeft, (float)CornerRadius.BottomLeft)
                    });
                    lease.SkCanvas.DrawRoundRect(round, paint);
                }
            }
        }
    }
}
