using System.Threading.Tasks;
using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Input.Platform
{
    [PrivateApi]
    public interface IPlatformDragSource
    {
        Task<DragDropEffects> DoDragDropAsync(
            PointerPressedEventArgs triggerEvent,
            IDataTransfer dataTransfer,
            DragDropEffects allowedEffects);
    }
}
