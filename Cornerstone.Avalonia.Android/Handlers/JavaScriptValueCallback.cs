#region References

using System;
using Android.Webkit;
using Object = Java.Lang.Object;

#endregion

namespace Cornerstone.Avalonia.Android.Handlers;

internal class JavaScriptValueCallback : Object, IValueCallback
{
	#region Fields

	private readonly Action<Object> _callback;

	#endregion

	#region Constructors

	public JavaScriptValueCallback(Action<Object> callback)
	{
		ArgumentNullException.ThrowIfNull(callback);
		_callback = callback;
	}

	#endregion

	#region Methods

	public void OnReceiveValue(Object value)
	{
		_callback?.Invoke(value);
	}

	#endregion
}