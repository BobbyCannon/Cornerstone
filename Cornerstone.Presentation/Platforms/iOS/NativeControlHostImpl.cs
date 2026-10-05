using System;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Platform;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace Cornerstone.Presentation.iOS
{
    internal class NativeControlHostImpl : INativeControlHostImpl
    {
        private readonly CornerstoneView _cornerstoneView;

        public NativeControlHostImpl(CornerstoneView cornerstoneView)
        {
            _cornerstoneView = cornerstoneView;
        }

        public INativeControlHostDestroyableControlHandle CreateDefaultChild(IPlatformHandle parent)
        {
            return new UIViewControlHandle(new UIView());
        }

        public INativeControlHostControlTopLevelAttachment CreateNewAttachment(Func<IPlatformHandle, IPlatformHandle> create)
        {
            var parent = new UIViewControlHandle(_cornerstoneView);
            NativeControlAttachment? attachment = null;
            try
            {
                var child = create(parent);
                // It has to be assigned to the variable before property setter is called so we dispose it on exception
#pragma warning disable IDE0017 // Simplify object initialization
                attachment = new NativeControlAttachment(child);
#pragma warning restore IDE0017 // Simplify object initialization
                attachment.AttachedTo = this;
                return attachment;
            }
            catch
            {
                attachment?.Dispose();
                throw;
            }
        }

        public INativeControlHostControlTopLevelAttachment CreateNewAttachment(IPlatformHandle handle)
        {
            return new NativeControlAttachment(handle)
            {
                AttachedTo = this
            };
        }

        public bool IsCompatibleWith(IPlatformHandle handle) => handle.HandleDescriptor == UIViewControlHandle.UIViewDescriptor;

        private class ViewHolder : UIView
        {
            public ViewHolder(IntPtr handle) : base(new NativeHandle(handle))
            {
                
            }
        }
        
        private class NativeControlAttachment : INativeControlHostControlTopLevelAttachment
        {
            // ReSharper disable once NotAccessedField.Local (keep GC reference)
            private IPlatformHandle? _child;
            private UIView? _view;
            private NativeControlHostImpl? _attachedTo;

            public NativeControlAttachment(IPlatformHandle child)
            {
                _child = child;
                
                _view = (child as UIViewControlHandle)?.View ?? new ViewHolder(child.Handle);
            }

            [MemberNotNull(nameof(_view))]
            private void CheckDisposed()
            {
                if (_view == null)
                    throw new ObjectDisposedException(nameof(NativeControlAttachment));
            }

            public void Dispose()
            {
                _view?.RemoveFromSuperview();
                _child = null;
                _attachedTo = null;
                _view?.Dispose();
                _view = null;
            }

            public INativeControlHostImpl? AttachedTo
            {
                get => _attachedTo;
                set
                {
                    CheckDisposed();
                    
                    _attachedTo = (NativeControlHostImpl?)value;
                    if (_attachedTo == null)
                    {
                        _view.RemoveFromSuperview();
                    }
                    else if (NativeAirspace.BehindComposition)
                    {
                        _attachedTo._cornerstoneView.AttachNativeIsland(_view);
                    }
                    else
                    {
                        _attachedTo._cornerstoneView.AddSubview(_view);
                    }
                }
            }

            public bool IsCompatibleWith(INativeControlHostImpl host) => host is NativeControlHostImpl;

            public void HideWithSize(Size size)
            {
                CheckDisposed();
                if (_attachedTo == null)
                    return;

                _view.Hidden = true;
                _view.Frame = new CGRect(0d, 0d, Math.Max(1d, size.Width), Math.Max(1d, size.Height));
            }

            public void ShowInBounds(Rect bounds)
            {
                CheckDisposed();
                if (_attachedTo == null)
                    throw new InvalidOperationException("The control isn't currently attached to a toplevel");

                var rect = new CGRect(bounds.X, bounds.Y, Math.Max(1d, bounds.Width), Math.Max(1d, bounds.Height));
                if (NativeAirspace.BehindComposition)
                {
                    var host = _attachedTo._cornerstoneView;
                    if (host.Superview != null)
                        rect = host.ConvertRectToView(rect, host.Superview);
                }

                if (_view.Frame != rect)
                {
                    var scroll = FindScrollView(_view);
                    var offset = scroll?.ContentOffset ?? default;
                    _view.Frame = rect;
                    if (scroll != null && scroll.ContentOffset != offset)
                        scroll.ContentOffset = offset;
                }
                _view.Hidden = false;
            }

            private static UIScrollView FindScrollView(UIView view)
            {
                if (view is UIScrollView scrollView)
                    return scrollView;
                foreach (var child in view.Subviews)
                {
                    if (child is UIScrollView childScroll)
                        return childScroll;
                }

                return null;
            }
        }
    }

    public class UIViewControlHandle : PlatformHandle, INativeControlHostDestroyableControlHandle
    {
        internal const string UIViewDescriptor = "UIView";

        public UIViewControlHandle(UIView view) : base(view.Handle.Handle, UIViewDescriptor)
        {
            View = view;
        }
        
        public UIView View { get; }

        public void Destroy()
        {
            View.Dispose();
        }
    }
}
