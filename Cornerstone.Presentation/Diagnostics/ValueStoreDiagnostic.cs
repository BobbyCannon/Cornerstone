using System.Collections.Generic;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Styling;

namespace Cornerstone.Presentation.Diagnostics;

[PrivateApi]
public class ValueStoreDiagnostic
{
    /// <summary>
    /// Currently applied frames.
    /// </summary>
    public IReadOnlyList<IValueFrameDiagnostic> AppliedFrames { get; }

    internal ValueStoreDiagnostic(IReadOnlyList<IValueFrameDiagnostic> appliedFrames)
    {
        AppliedFrames = appliedFrames;
    }
}
