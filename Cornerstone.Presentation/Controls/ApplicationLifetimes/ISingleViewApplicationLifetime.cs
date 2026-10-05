using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Controls.Elements;

namespace Cornerstone.Presentation.Controls.ApplicationLifetimes
{
    [NotClientImplementable]
    public interface ISingleViewApplicationLifetime : IApplicationLifetime
    {
        Control? MainView { get; set; }
    }
}
