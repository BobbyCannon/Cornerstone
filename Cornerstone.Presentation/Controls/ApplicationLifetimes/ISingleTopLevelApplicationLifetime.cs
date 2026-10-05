using System.ComponentModel;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Controls.Chrome;

namespace Cornerstone.Presentation.Controls.ApplicationLifetimes;

/// <summary>
/// Used in our internal projects. Until we figure out way to add this information to the public API. 
/// </summary>
[NotClientImplementable]
[PrivateApi]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface ISingleTopLevelApplicationLifetime : IApplicationLifetime
{
    TopLevel? TopLevel { get; }
}
