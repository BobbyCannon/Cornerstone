#region References

using System.Threading.Tasks;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Platform;
using Cornerstone.Reflection;
using Cornerstone.Presentation.Controls.Chrome;

#endregion

namespace Cornerstone.Presentation;

[SourceReflection]
public class ClipboardService
{
	#region Properties

	protected virtual IClipboard Clipboard => Application.Current?.ApplicationLifetime switch
	{
		IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow?.Clipboard,
		ISingleViewApplicationLifetime single => TopLevel.GetTopLevel(single.MainView)?.Clipboard,
		_ => null
	};

	#endregion

	#region Methods

	public Task ClearAsync()
	{
		var response = Clipboard?.ClearAsync() ?? Task.CompletedTask;
		return response;
	}

	public async Task<string> GetTextAsync()
	{
		var clipboard = Clipboard;
		if (clipboard == null)
		{
			return null;
		}

		var text = await clipboard.TryGetTextAsync().ConfigureAwait(true);
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}

		return await clipboard.TryGetValueAsync(DataFormat.Text).ConfigureAwait(true);
	}

	public Task SetTextAsync(string text)
	{
		var response = Clipboard?.SetTextAsync(text) ?? Task.CompletedTask;
		return response;
	}

	#endregion
}