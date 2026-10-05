using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Platform
{
    [PrivateApi]
    public interface IDrawingContextWithAcrylicLikeSupport
    {
        void DrawRectangle(IExperimentalAcrylicMaterial material, RoundedRect rect);
    }
}
