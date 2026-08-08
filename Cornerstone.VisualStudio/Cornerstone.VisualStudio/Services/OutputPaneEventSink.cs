#region References

using System;
using System.IO;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

#endregion

namespace Cornerstone.VisualStudio.Services;

/// <summary>
/// A serilog sink that outputs to the VS output window.
/// </summary>
internal class OutputPaneEventSink : ILogEventSink
{
	#region Fields

	private readonly ITextFormatter _formatter;
	private readonly IVsOutputWindowPane _pane;
	private static readonly Guid _paneGuid;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="OutputPaneEventSink" /> class.
	/// </summary>
	/// <param name="output"> The VS output window. </param>
	/// <param name="outputTemplate"> The serilog output template. </param>
	public OutputPaneEventSink(IVsOutputWindow output, string outputTemplate)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		_formatter = new MessageTemplateTextFormatter(outputTemplate);

		ErrorHandler.ThrowOnFailure(output.CreatePane(_paneGuid, "Cornerstone Diagnostics", 1, 1));
		output.GetPane(_paneGuid, out _pane);
	}

	static OutputPaneEventSink()
	{
		_paneGuid = new("DC845612-459C-485C-8157-71BC39C9A044");
	}

	#endregion

	#region Methods

	public void Emit(LogEvent logEvent)
	{
		var sw = new StringWriter();
		_formatter.Format(logEvent, sw);
		var message = sw.ToString();

		if (_pane is IVsOutputWindowPaneNoPump noPump)
		{
			noPump.OutputStringNoPump(message);
		}
		else
		{
			ErrorHandler.ThrowOnFailure(_pane.OutputStringThreadSafe(message));
		}

		if (logEvent.Level == LogEventLevel.Error)
		{
			_pane.Activate();
		}
	}

	#endregion
}