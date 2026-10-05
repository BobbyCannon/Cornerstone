using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Cornerstone.Presentation.Browser.Interop;

internal static partial class StorageHelper
{
    [JSImport("Caniuse.hasNativeFilePicker", CornerstoneModule.MainModuleName)]
    public static partial bool HasNativeFilePicker();

    [JSImport("StorageProvider.selectFolderDialog", CornerstoneModule.StorageModuleName)]
    public static partial Task<JSObject?> SelectFolderDialog(JSObject? startIn, bool preferPolyfill);

    [JSImport("StorageProvider.openFileDialog", CornerstoneModule.StorageModuleName)]
    public static partial Task<JSObject?> OpenFileDialog(JSObject? startIn, bool multiple,
        [JSMarshalAs<JSType.Array<JSType.Any>>] object[]? types, bool excludeAcceptAllOption, bool preferPolyfill);

    [JSImport("StorageProvider.saveFileDialog", CornerstoneModule.StorageModuleName)]
    public static partial Task<JSObject?> SaveFileDialog(JSObject? startIn, string? suggestedName,
        [JSMarshalAs<JSType.Array<JSType.Any>>] object[]? types, bool excludeAcceptAllOption, bool preferPolyfill);

    [JSImport("StorageItem.createWellKnownDirectory", CornerstoneModule.StorageModuleName)]
    public static partial JSObject CreateWellKnownDirectory(string wellKnownDirectory);
    
    [JSImport("StorageProvider.openBookmark", CornerstoneModule.StorageModuleName)]
    public static partial Task<JSObject?> OpenBookmark(string key);

    [JSImport("StorageItem.saveBookmark", CornerstoneModule.StorageModuleName)]
    public static partial Task<string?> SaveBookmark(JSObject item);

    [JSImport("StorageItem.deleteBookmark", CornerstoneModule.StorageModuleName)]
    public static partial Task DeleteBookmark(JSObject item);

    [JSImport("StorageItem.getProperties", CornerstoneModule.StorageModuleName)]
    public static partial Task<JSObject?> GetProperties(JSObject item);

    [JSImport("StorageItem.openWrite", CornerstoneModule.StorageModuleName)]
    public static partial Task<JSObject> OpenWrite(JSObject item);

    [JSImport("StorageItem.openRead", CornerstoneModule.StorageModuleName)]
    public static partial Task<JSObject> OpenRead(JSObject item);
    
    [JSImport("StorageItem.createFromHandle", CornerstoneModule.StorageModuleName)]
    public static partial JSObject? StorageItemFromHandle(JSObject handle);

    [JSImport("StorageItem.getItemsIterator", CornerstoneModule.StorageModuleName)]
    [return: JSMarshalAs<JSType.Object>]
    public static partial JSObject? GetItemsIterator(JSObject item);

    [JSImport("StorageItems.itemsArray", CornerstoneModule.StorageModuleName)]
    public static partial JSObject[] ItemsArray(JSObject item);
    
    [JSImport("StorageItems.filesToItemsArray", CornerstoneModule.StorageModuleName)]
    public static partial JSObject[] FilesToItemsArray(JSObject item);

    [JSImport("StorageProvider.createAcceptType", CornerstoneModule.StorageModuleName)]
    public static partial JSObject CreateAcceptType(string description, string[] mimeTypes, string[]? extensions);

    [JSImport("StorageItem.deleteAsync", CornerstoneModule.StorageModuleName)]
    public static partial Task DeleteAsync(JSObject fileHandle);
    
    [JSImport("StorageItem.moveAsync", CornerstoneModule.StorageModuleName)]
    public static partial Task<JSObject?> MoveAsync(JSObject fileHandle, JSObject destinationFolder);
    
    [JSImport("StorageItem.createFile", CornerstoneModule.StorageModuleName)]
    public static partial Task<JSObject?> CreateFile(JSObject folderHandle, string name);
    
    [JSImport("StorageItem.createFolder", CornerstoneModule.StorageModuleName)]
    public static partial Task<JSObject?> CreateFolder(JSObject folderHandle, string name);

    [JSImport("StorageItem.getFile", CornerstoneModule.StorageModuleName)]
    public static partial Task<JSObject?> GetFile(JSObject folderHandle, string name);

    [JSImport("StorageItem.getFolder", CornerstoneModule.StorageModuleName)]
    public static partial Task<JSObject?> GetFolder(JSObject folderHandle, string name);
}
