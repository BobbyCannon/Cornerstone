using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cornerstone.Presentation.Platform.Storage.FileIO;

namespace Cornerstone.Presentation.Platform.Storage;

internal class NoopStorageProvider : BclStorageProvider
{
    public override bool CanOpen => false;

    public override Task<OpenFilePickerResult> OpenFilePickerWithResultAsync(FilePickerOpenOptions options)
    {
        return Task.FromResult(new OpenFilePickerResult());
    }

    public override bool CanSave => false;

    public override Task<SaveFilePickerResult> SaveFilePickerWithResultAsync(FilePickerSaveOptions options)
    {
        return Task.FromResult(new SaveFilePickerResult());
    }

    public override bool CanPickFolder => false;

    public override Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
    {
        return Task.FromResult<IReadOnlyList<IStorageFolder>>(Array.Empty<IStorageFolder>());
    }
}
