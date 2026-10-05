using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Platform;
using Cornerstone.Presentation.Platforms.MacOS.Interop;
using Cornerstone.Presentation.Controls.Chrome;

namespace Cornerstone.Presentation.Platforms.MacOS
{
    class MacDragSource : IPlatformDragSource
    {
        private readonly ICornerstoneNativeFactory _factory;

        public MacDragSource(ICornerstoneNativeFactory factory)
        {
            _factory = factory;
        }

        class DndCallback : NativeCallbackBase, ICsnDndResultCallback
        {
            private TaskCompletionSource<DragDropEffects>? _tcs;

            public DndCallback(TaskCompletionSource<DragDropEffects> tcs)
            {
                _tcs = tcs;
            }
            public void OnDragAndDropComplete(CsnDragDropEffects effect)
            {
                _tcs?.TrySetResult((DragDropEffects)effect);
                _tcs = null;
            }
        }

        public Task<DragDropEffects> DoDragDropAsync(
            PointerPressedEventArgs triggerEvent,
            IDataTransfer dataTransfer,
            DragDropEffects allowedEffects)
        {
            // Sanity check
            var tl = TopLevel.GetTopLevel(triggerEvent.Source as Visual);
            var view = tl?.PlatformImpl as TopLevelImpl;
            if (view == null)
                throw new ArgumentException();

            triggerEvent.Pointer.Capture(null);
            
            var tcs = new TaskCompletionSource<DragDropEffects>();

            using (var cb = new DndCallback(tcs))
            {
                var dataSource = new DataTransferToCsnClipboardDataSourceWrapper(dataTransfer);

                view.BeginDraggingSession((CsnDragDropEffects)allowedEffects,
                    triggerEvent.GetPosition(tl).ToCsnPoint(), dataSource, cb,
                    GCHandle.ToIntPtr(GCHandle.Alloc(dataTransfer)));
            }

            return tcs.Task;
        }
    }
}
