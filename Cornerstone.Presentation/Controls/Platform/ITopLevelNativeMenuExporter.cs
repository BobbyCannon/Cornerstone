using System;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Controls.Chrome;

namespace Cornerstone.Presentation.Controls.Platform
{
    [Unstable]
    public interface INativeMenuExporter
    {
        void SetNativeMenu(NativeMenu? menu);
    }

    [Unstable]
    public interface ITopLevelNativeMenuExporter : INativeMenuExporter
    {
        bool IsNativeMenuExported { get; }

        event EventHandler OnIsNativeMenuExportedChanged;
    }

    [Unstable]
    public interface INativeMenuExporterProvider
    {
        INativeMenuExporter? NativeMenuExporter { get; }
    }
}
