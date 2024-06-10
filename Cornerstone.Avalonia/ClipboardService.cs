#region References

using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Avalonia;

public class ClipboardService : IClipboardService
{
	#region Fields

	private IClipboard _clipboard;

	#endregion

	#region Properties

	protected virtual IClipboard Clipboard => _clipboard ??= Application.Current.GetTopLevel()?.Clipboard;

	#endregion

	#region Methods

	public Task ClearAsync()
	{
		return Clipboard?.ClearAsync() ?? Task.CompletedTask;
	}

	public Task<object> GetDataAsync(string format)
	{
		return Clipboard?.GetDataAsync(format) ?? Task.FromResult<object>(null);
	}

	public Task<string[]> GetFormatsAsync()
	{
		return Clipboard?.GetFormatsAsync() ?? Task.FromResult(Array.Empty<string>());
	}

	public Task<string> GetTextAsync()
	{
		return Clipboard?.GetTextAsync() ?? Task.FromResult<string>(null);
	}

	public Task SetDataObjectAsync(IDataObject data)
	{
		return Clipboard?.SetDataObjectAsync(data) ?? Task.CompletedTask;
	}

	public Task SetTextAsync(string text)
	{
		return Clipboard?.SetTextAsync(text) ?? Task.CompletedTask;
	}

	#endregion
}