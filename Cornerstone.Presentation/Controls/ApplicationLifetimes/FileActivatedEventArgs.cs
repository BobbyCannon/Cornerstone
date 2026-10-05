using System.Collections.Generic;
using Cornerstone.Presentation.Platform.Storage;

namespace Cornerstone.Presentation.Controls.ApplicationLifetimes;

public sealed class FileActivatedEventArgs : ActivatedEventArgs
{
    public FileActivatedEventArgs(IReadOnlyList<IStorageItem> files) : base(ActivationKind.File)
    {
        Files = files;
    }

    public IReadOnlyList<IStorageItem> Files { get; }
}
