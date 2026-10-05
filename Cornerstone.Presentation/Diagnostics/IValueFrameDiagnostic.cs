using System.Collections.Generic;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Diagnostics;

[PrivateApi]
public record ValueEntryDiagnostic(PresentationProperty Property, object? Value);

[PrivateApi]
[NotClientImplementable]
public interface IValueFrameDiagnostic
{
    public enum FrameType
    {
        Unknown = 0,
        Local,
        Theme,
        Style,
        Template
    }

    object? Source { get; } 
    FrameType Type { get; }
    bool IsActive { get; }
    BindingPriority Priority { get; }
    IEnumerable<ValueEntryDiagnostic> Values { get; } 
}
