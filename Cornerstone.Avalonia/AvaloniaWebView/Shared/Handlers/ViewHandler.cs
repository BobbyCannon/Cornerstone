#region References

using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Platform;
using Cornerstone.Avalonia.AvaloniaWebView.Core;
using PropertyChanging;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView.Shared.Handlers;

[DoNotNotify]
[PropertyChanged.DoNotNotify]
public abstract class ViewHandler : NativeControlHost, IViewHandler, INativeControlHostDestroyableControlHandle, IPlatformHandle, IDisposable
{
	#region Fields

	private bool _disposedValue;

	#endregion

	#region Constructors

	~ViewHandler()
	{
		Dispose(false);
	}

	#endregion

	#region Properties

	//#nullable restore

	public Control AttachableControl => this;

	public IntPtr Handle => RefHandler.Handle;

	public string HandleDescriptor { get; protected set; }

	//#nullable disable
	public object PlatformViewContextObject { get; protected set; } = default!;
	public IPlatformWebView PlatformWebView { get; protected set; } = default!;
	public HandleRef RefHandler { get; private set; }
	public object VisualViewContextObject { get; protected set; } = default!;

	#endregion

	#region Methods

	public void Destroy()
	{
		((IDisposable) this).Dispose();
	}

	protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
	{
		var nativeHandle = CreatePlatformHandler(parent, () => base.CreateNativeControlCore(parent));
		RefHandler = nativeHandle;
		//PlatformHandlerChanged?.Invoke(this, EventArgs.Empty);
		OnPlatformHandlerChanged();
		return this;
	}

	protected abstract HandleRef CreatePlatformHandler(IPlatformHandle parent, Func<IPlatformHandle> createFromSystem);

	protected override void DestroyNativeControlCore(IPlatformHandle control)
	{
		((IDisposable) this).Dispose();
		base.DestroyNativeControlCore(control);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				Disposing();
			}

			_disposedValue = true;
		}
	}

	protected abstract void Disposing();

	protected virtual void OnPlatformHandlerChanged()
	{
		PlatformHandlerChanged?.Invoke(this, EventArgs.Empty);
	}

	void IDisposable.Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	#endregion

	#region Events

	public event EventHandler PlatformHandlerChanged;

	#endregion
}