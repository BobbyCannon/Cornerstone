using System;
using System.Threading.Tasks;
using Cornerstone.Presentation.Browser.Interop;
using Cornerstone.Presentation.Platform.Storage;

namespace Cornerstone.Presentation.Browser.Storage;

internal class BrowserLauncher : ILauncher
{
    public Task<bool> LaunchUriAsync(Uri uri)
    {
        _ = uri ?? throw new ArgumentNullException(nameof(uri));

        if (uri.IsAbsoluteUri)
        {
            return Task.FromResult(NavigationHelper.WindowOpen(uri.AbsoluteUri, "_blank"));
        }
        return Task.FromResult(false);
    }

    public Task<bool> LaunchFileAsync(IStorageItem storageItem)
    {
        _ = storageItem ?? throw new ArgumentNullException(nameof(storageItem));

        return Task.FromResult(false);
    }
}
