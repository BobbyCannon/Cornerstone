using System;
using Cornerstone.Presentation.Logging;
using Microsoft.Extensions.Logging;

namespace Cornerstone.RemoteLink.Vnc.Controls.Adapters.Logging;

/// <summary>
/// Logging implementation that forwards log output to Cornerstone logging sinks.
/// </summary>
public class PresentationLogger : ILogger
{
	private const string AreaName = "VncClient";
	private readonly string _categoryName;

	internal PresentationLogger(string categoryName)
	{
		_categoryName = categoryName;
	}

	/// <inheritdoc />
	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
	{
		if (formatter == null)
		{
			throw new ArgumentNullException(nameof(formatter));
		}

		var logEventLevel = GetLogEventLevel(logLevel);
		if (logEventLevel == null)
		{
			return;
		}

		if (!Logger.TryGet(logEventLevel.Value, AreaName, out var outLogger))
		{
			return;
		}

		var message = $"{_categoryName}: {formatter(state, exception)}";
		if (exception != null)
		{
			message += Environment.NewLine + exception + Environment.NewLine;
		}

		outLogger.Log(this, message);
	}

	/// <inheritdoc />
	public bool IsEnabled(LogLevel logLevel)
	{
		var logEventLevel = GetLogEventLevel(logLevel);
		return (logEventLevel != null) && Logger.IsEnabled(logEventLevel.Value, AreaName);
	}

	/// <inheritdoc />
	public IDisposable BeginScope<TState>(TState state)
	{
		return NullScope.Instance;
	}

	private static LogEventLevel? GetLogEventLevel(LogLevel logLevel)
	{
		return logLevel switch
		{
			LogLevel.Trace => LogEventLevel.Verbose,
			LogLevel.Debug => LogEventLevel.Debug,
			LogLevel.Information => LogEventLevel.Information,
			LogLevel.Warning => LogEventLevel.Warning,
			LogLevel.Error => LogEventLevel.Error,
			LogLevel.Critical => LogEventLevel.Fatal,
			LogLevel.None => null,
			_ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
		};
	}

	public class NullScope : IDisposable
	{
		public static NullScope Instance { get; } = new();

		private NullScope()
		{
		}

		public void Dispose()
		{
		}
	}
}
