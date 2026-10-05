using System.Threading.Tasks;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Platform;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.Platforms.Windows.Interop;
using MicroCom.Runtime;

namespace Cornerstone.Presentation.Platforms.Windows
{
    internal sealed class DragSource : IPlatformDragSource
    {
        public Task<DragDropEffects> DoDragDropAsync(
            PointerPressedEventArgs triggerEvent,
            IDataTransfer dataTransfer,
            DragDropEffects allowedEffects)
        {
            Dispatcher.UIThread.VerifyAccess();

            triggerEvent.Pointer.Capture(null);
            
            using var dataObject = new DataTransferToOleDataObjectWrapper(dataTransfer);
            using var src = new OleDragSource();
            var allowed = OleDropTarget.ConvertDropEffect(allowedEffects);
            
            var objPtr = dataObject.GetNativeIntPtr<Win32Com.IDataObject>();
            var srcPtr = src.GetNativeIntPtr<Win32Com.IDropSource>();

            UnmanagedMethods.DoDragDrop(objPtr, srcPtr, (int)allowed, out var finalEffect);
            
            // Force releasing of internal wrapper to avoid memory leak, if drop target keeps com reference.
            dataObject.ReleaseDataTransfer();

            return Task.FromResult(OleDropTarget.ConvertDropEffect((Win32Com.DropEffect)finalEffect));
        }
    }
}
