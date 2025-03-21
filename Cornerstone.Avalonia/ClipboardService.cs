#region References

using System;
using System.Security;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Profiling;

#endregion

namespace Cornerstone.Avalonia;

public class ClipboardService : IClipboardService, IDisposable
{
	#region Fields

	private readonly DebounceService _clearDebounce;
	private IClipboard _clipboard;

	#endregion

	#region Constructors

	public ClipboardService()
	{
		_clearDebounce = new DebounceService(TimeSpan.FromSeconds(5), _ => ClearAsync());
	}

	#endregion

	#region Properties

	protected virtual IClipboard Clipboard => _clipboard ??= Application.Current.GetTopLevel()?.Clipboard;

	#endregion

	#region Methods

	public Task ClearAsync()
	{
		var response = Clipboard?.ClearAsync() ?? Task.CompletedTask;
		_clearDebounce.Cancel();
		return response;
	}

	public void Dispose()
	{
		_clearDebounce?.Dispose();
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

	public void QueueClear()
	{
		_clearDebounce.Trigger();
	}

	public Task SetDataObjectAsync(IDataObject data)
	{
		var response = Clipboard?.SetDataObjectAsync(data) ?? Task.CompletedTask;
		_clearDebounce.Cancel();
		return response;
	}

	public Task SetTextAsync(string text)
	{
		var response = Clipboard?.SetTextAsync(text) ?? Task.CompletedTask;
		_clearDebounce.Cancel();
		return response;
	}

	public Task SetTextAsync(SecureString text)
	{
		var response = Clipboard?.SetTextAsync(text.ToUnsecureString()) ?? Task.CompletedTask;
		_clearDebounce.Trigger();
		return response;
	}

	#endregion
}