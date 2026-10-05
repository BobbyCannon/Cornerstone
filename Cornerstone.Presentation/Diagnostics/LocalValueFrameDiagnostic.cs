using System.Collections.Generic;
using Cornerstone.Presentation.Data;

namespace Cornerstone.Presentation.Diagnostics;

internal class LocalValueFrameDiagnostic : IValueFrameDiagnostic
{
    public LocalValueFrameDiagnostic(IEnumerable<ValueEntryDiagnostic> values)
    {
        Values = values;
    }

    public object? Source => null;
    public IValueFrameDiagnostic.FrameType Type => IValueFrameDiagnostic.FrameType.Local;
    public bool IsActive => true;
    public BindingPriority Priority => BindingPriority.LocalValue;
    public IEnumerable<ValueEntryDiagnostic> Values { get; }
}
