#region References

using System;
using System.Threading.Tasks;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Platform;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests;

[TestClass]
public class ClipboardServiceTests
{
	#region Methods

	[TestMethod]
	public async Task TimedCopyClearsWhenNothingReplacesIt()
	{
		var clipboard = new MemoryClipboard();
		var service = new TestClipboardService(clipboard);

		await service.SetTextAsync("secret", TimeSpan.FromMilliseconds(40));

		CornerstoneTest.IsNull(await service.GetTextAsync());
		CornerstoneTest.AreEqual(1, clipboard.ClearCount);
	}

	[TestMethod]
	public async Task LaterCopyOfTheSameTextIsNotCleared()
	{
		var clipboard = new MemoryClipboard();
		var service = new TestClipboardService(clipboard);

		var timed = service.SetTextAsync("secret", TimeSpan.FromMilliseconds(80));
		await service.SetTextAsync("secret");
		await timed;

		CornerstoneTest.AreEqual("secret", await service.GetTextAsync());
		CornerstoneTest.AreEqual(0, clipboard.ClearCount);
	}

	[TestMethod]
	public async Task CopyDuringTheFirstPublishIsNotCleared()
	{
		var clipboard = new MemoryClipboard();
		clipboard.HoldNextSet = true;
		var service = new TestClipboardService(clipboard);

		var timed = service.SetTextAsync("secret", TimeSpan.FromSeconds(30));
		await service.SetTextAsync("secret");
		clipboard.ReleaseSet();
		await timed;

		CornerstoneTest.AreEqual("secret", await service.GetTextAsync());
		CornerstoneTest.AreEqual(0, clipboard.ClearCount);
	}

	[TestMethod]
	public async Task ClearOwnedRemovesTheTimedCopy()
	{
		var clipboard = new MemoryClipboard();
		var service = new TestClipboardService(clipboard);

		var timed = service.SetTextAsync("secret", TimeSpan.FromMilliseconds(40));
		await service.ClearOwned();
		await timed;

		CornerstoneTest.IsNull(await service.GetTextAsync());
	}

	#endregion

	#region Classes

	private sealed class MemoryClipboard : IClipboard
	{
		private IAsyncDataTransfer _data;
		private TaskCompletionSource<bool> _setGate;

		public int ClearCount { get; private set; }

		public bool HoldNextSet { get; set; }

		public Task ClearAsync()
		{
			ClearCount++;
			_data = null;
			return Task.CompletedTask;
		}

		public Task FlushAsync()
		{
			return Task.CompletedTask;
		}

		public void ReleaseSet()
		{
			_setGate?.TrySetResult(true);
		}

		public Task SetDataAsync(IAsyncDataTransfer dataTransfer)
		{
			_data = dataTransfer;
			if (!HoldNextSet)
			{
				return Task.CompletedTask;
			}

			HoldNextSet = false;
			_setGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			return _setGate.Task;
		}

		public Task<IAsyncDataTransfer> TryGetDataAsync()
		{
			return Task.FromResult(_data);
		}

		public Task<IAsyncDataTransfer> TryGetInProcessDataAsync()
		{
			return Task.FromResult(_data);
		}
	}

	private sealed class TestClipboardService : ClipboardService
	{
		private readonly IClipboard _clipboard;

		public TestClipboardService(IClipboard clipboard)
		{
			_clipboard = clipboard;
		}

		protected override IClipboard Clipboard => _clipboard;
	}

	#endregion
}
