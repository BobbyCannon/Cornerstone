#region References

using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input.Platform;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Avalonia;

[SourceReflection]
public class ClipboardService
{
	#region Properties

	protected virtual IClipboard Clipboard => field ??= Application.Current.GetTopLevel()?.Clipboard;

	#endregion

	#region Methods

	public Task ClearAsync()
	{
		var response = Clipboard?.ClearAsync() ?? Task.CompletedTask;
		return response;
	}

	public Task<string> GetTextAsync()
	{
		return Clipboard?.TryGetTextAsync() ?? Task.FromResult<string>(null);
	}

	public Task SetTextAsync(string text)
	{
		var response = Clipboard?.SetTextAsync(text) ?? Task.CompletedTask;
		return response;
	}

	#endregion
}