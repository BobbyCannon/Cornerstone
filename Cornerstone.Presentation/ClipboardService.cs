#region References

using System;
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
	#region Fields

	// Set by ClearOwned so a timed publish that lands after that clear is still removed.
	// SetTextAsync clears it. A later copy is not a dropped secret.
	private bool _clearLatePublish;
	private int _generation;
	private string _ownedText;

	#endregion

	#region Constructors

	public ClipboardService()
	{
		_clearLatePublish = false;
		_generation = 0;
		_ownedText = string.Empty;
	}

	#endregion

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

	/// <summary>
	/// Clears the clipboard when it still holds the last timed copy.
	/// A copy that did not ask for a timeout is left alone.
	/// </summary>
	public Task ClearOwned()
	{
		var owned = _ownedText;
		if (string.IsNullOrEmpty(owned))
		{
			return Task.CompletedTask;
		}

		_generation++;
		var generation = _generation;
		_ownedText = string.Empty;
		_clearLatePublish = true;
		return ClearIfStillOwnedAsync(owned, generation);
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
		return SetTextAsync(text, TimeSpan.Zero);
	}

	/// <summary>
	/// Puts text on the clipboard. When clearAfter is positive, clears that same text after the delay if it is still there.
	/// </summary>
	public Task SetTextAsync(string text, TimeSpan clearAfter)
	{
		_generation++;
		_clearLatePublish = false;
		if ((clearAfter <= TimeSpan.Zero) || string.IsNullOrEmpty(text))
		{
			_ownedText = string.Empty;
			return Clipboard?.SetTextAsync(text) ?? Task.CompletedTask;
		}

		var generation = _generation;
		_ownedText = text;
		return PublishAndClearAsync(text, generation, clearAfter);
	}

	private async Task ClearIfStillOwnedAsync(string owned, int generation)
	{
		try
		{
			var current = await GetTextAsync().ConfigureAwait(true);
			if ((generation != _generation) || !string.Equals(current, owned, StringComparison.Ordinal))
			{
				return;
			}

			await ClearAsync().ConfigureAwait(true);
		}
		catch (Exception)
		{
			// Another process can lock the clipboard.
		}
	}

	private async Task PublishAndClearAsync(string text, int generation, TimeSpan clearAfter)
	{
		try
		{
			var set = Clipboard?.SetTextAsync(text) ?? Task.CompletedTask;
			await set.ConfigureAwait(true);
			if (generation != _generation)
			{
				// ClearOwned looked before this publish finished. A later copy clears the flag and stays.
				if (_clearLatePublish)
				{
					var late = await GetTextAsync().ConfigureAwait(true);
					if (string.Equals(late, text, StringComparison.Ordinal))
					{
						await ClearAsync().ConfigureAwait(true);
					}
				}

				return;
			}

			await Task.Delay(clearAfter).ConfigureAwait(true);
			if ((generation != _generation) || !string.Equals(_ownedText, text, StringComparison.Ordinal))
			{
				return;
			}

			var current = await GetTextAsync().ConfigureAwait(true);
			if (!string.Equals(current, text, StringComparison.Ordinal))
			{
				return;
			}

			await ClearAsync().ConfigureAwait(true);
			if (generation == _generation)
			{
				_ownedText = string.Empty;
			}
		}
		catch (Exception)
		{
			// Another process can lock the clipboard.
		}
	}

	#endregion
}