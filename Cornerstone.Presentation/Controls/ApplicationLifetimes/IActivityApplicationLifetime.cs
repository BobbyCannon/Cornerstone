using System;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Controls.Elements;

namespace Cornerstone.Presentation.Controls.ApplicationLifetimes
{
    [NotClientImplementable]
    public interface IActivityApplicationLifetime : IApplicationLifetime
    {
        Func<Control>? MainViewFactory { get; set; }
    }
}
