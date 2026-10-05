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
	private static readonly Guid PaneGuid = new("DC845612-459C-485C-8157-71BC39C9A044");

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

		var guid = PaneGuid;
		// Recreate so a renamed pane (e.g. old "Cornerstone Diagnostics") picks up the new caption.
		output.DeletePane(ref guid);
		// Do not clear on solution load — that wipes the initialized line before you open AXAML.
		ErrorHandler.ThrowOnFailure(output.CreatePane(ref guid, "Cornerstone", 1, 0));
		ErrorHandler.ThrowOnFailure(output.GetPane(ref guid, out _pane));
		_pane.Activate();
	}

	#endregion

	#region Methods

	public void Emit(LogEvent logEvent)
	{
		var sw = new StringWriter();
		_formatter.Format(logEvent, sw);
		var message = sw.ToString();

		ErrorHandler.ThrowOnFailure(_pane.OutputStringThreadSafe(message));
	}

	#endregion
}