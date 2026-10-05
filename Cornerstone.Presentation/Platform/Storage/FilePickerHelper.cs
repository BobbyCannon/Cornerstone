using System.Linq;
using System.Threading.Tasks;
using Cornerstone.Runtime;

namespace Cornerstone.Presentation.Platform.Storage;

/// <summary>
/// Convenience wrappers over <see cref="IStorageProvider"/> for single-path file and folder pickers.
/// </summary>
public static class FilePickerHelper
{
    public static async Task<string> TryOpenFileAsync(
        string startingDirectory = null,
        params FilePickerFileType[] pickerTypes)
    {
        var topLevel = Application.GetTopLevel();
        if (topLevel == null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(startingDirectory))
        {
            startingDirectory = AppBootstrap.RuntimeInformation.ApplicationDataLocation;
        }

        var defaultDirectory = await topLevel.StorageProvider.TryGetFolderFromPathAsync(startingDirectory);
        var options = new FilePickerOpenOptions
        {
            AllowMultiple = false,
            SuggestedStartLocation = defaultDirectory,
            FileTypeFilter = pickerTypes
        };

        var selected = await topLevel.StorageProvider.OpenFilePickerAsync(options);
        return selected.Count == 1 ? selected[0].Path.LocalPath : null;
    }

    public static async Task<string> TrySelectFileForSave(
        string startingDirectory = null,
        string defaultExtension = null,
        params FilePickerFileType[] fileTypeChoices)
    {
        var topLevel = Application.GetTopLevel();
        if (topLevel == null)
        {
            return null;
        }

        startingDirectory ??= AppBootstrap.RuntimeInformation.ApplicationDataLocation;

        var defaultDirectory = await topLevel.StorageProvider.TryGetFolderFromPathAsync(startingDirectory);
        var options = new FilePickerSaveOptions
        {
            SuggestedStartLocation = defaultDirectory,
            SuggestedFileType = defaultExtension == null ? null : fileTypeChoices.FirstOrDefault(x => x.Patterns.Any(p => p.EndsWith($".{defaultExtension}"))),
            FileTypeChoices = fileTypeChoices,
            DefaultExtension = defaultExtension
        };

        var selected = await topLevel.StorageProvider.SaveFilePickerAsync(options);
        return selected?.TryGetLocalPath();
    }

    public static async Task<string> TrySelectFolderAsync(string startingDirectory = null)
    {
        var topLevel = Application.GetTopLevel();
        if (topLevel == null)
        {
            return null;
        }

        var options = new FolderPickerOpenOptions { AllowMultiple = false, Title = "Select Folder" };
        var selected = await topLevel.StorageProvider.OpenFolderPickerAsync(options);
        var path = selected!.FirstOrDefault();
        var response = path?.TryGetLocalPath() ?? path?.Path.ToString();
        return response;
    }
}
