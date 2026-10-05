#region References

using System;
using System.Reactive.Disposables;
using Cornerstone.Presentation.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

public delegate void LogCallback(
	LogEventLevel level,
	string area,
	object source,
	string messageTemplate,
	params object[] propertyValues);

[TestClass]
public class TestLogSink : ILogSink
{
	#region Fields

	private readonly LogCallback _callback;

	#endregion

	#region Constructors

	public TestLogSink(LogCallback callback)
	{
		_callback = callback;
	}

	#endregion

	#region Methods

	public bool IsEnabled(LogEventLevel level, string area)
	{
		return true;
	}

	public void Log(LogEventLevel level, string area, object source, string messageTemplate)
	{
		_callback(level, area, source, messageTemplate);
	}

	public void Log(LogEventLevel level, string area, object source, string messageTemplate,
		params object[] propertyValues)
	{
		_callback(level, area, source, messageTemplate, propertyValues);
	}

	public static IDisposable Start(LogCallback callback)
	{
		var sink = new TestLogSink(callback);
		Logger.Sink = sink;
		return Disposable.Create(() => Logger.Sink = null);
	}

	#endregion
}