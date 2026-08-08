#region References

using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Remote.Protocol.Designer;
using Cornerstone.VisualStudio.Services;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;

#endregion

namespace Cornerstone.VisualStudio.IntelliSense;

internal class XamlErrorTagger : ITagger<IErrorTag>, ITableDataSource, IDisposable
{
	#region Fields

	private readonly ITextBuffer _buffer;
	private ExceptionDetails _error;
	private readonly ITextStructureNavigator _navigator;
	private readonly string _path;
	private readonly PreviewerProcess _process;
	private readonly string _projectName;
	private ITableDataSink _sink;
	private TagSpan<IErrorTag> _tagSpan;

	#endregion

	#region Constructors

	public XamlErrorTagger(
		ITableManagerProvider tableManagerProvider,
		ITextBuffer buffer,
		ITextStructureNavigator navigator,
		PreviewerProcess process)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		_buffer = buffer;
		_navigator = navigator;
		_process = process;
		_process.ErrorChanged += HandleErrorChanged;
		_error = process.Error;

		// Get the document path and containing project name.
		var document = GetDocument(buffer);
		_path = document?.FilePath;
		_projectName = GetProject(_path)?.Name;

		// Register ourselves with the error list.
		var tableManager = tableManagerProvider.GetTableManager(StandardTables.ErrorsTable);
		tableManager.AddSource(this,
			StandardTableColumnDefinitions.Column,
			StandardTableColumnDefinitions.DocumentName,
			StandardTableColumnDefinitions.ErrorSeverity,
			StandardTableColumnDefinitions.Line,
			StandardTableColumnDefinitions.Text);
	}

	#endregion

	#region Properties

	string ITableDataSource.DisplayName => "Avalonia XAML";
	string ITableDataSource.Identifier => "Avalonia XAML designer errors";

	string ITableDataSource.SourceTypeIdentifier => StandardTableDataSources.ErrorTableDataSource;

	#endregion

	#region Methods

	public void Dispose()
	{
		_sink?.RemoveAllEntries();

		if (_process != null)
		{
			_process.ErrorChanged -= HandleErrorChanged;
		}

		Disposed?.Invoke(this, EventArgs.Empty);
	}

	public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
	{
		var result = GetErrorTag(spans);

		if (result is not null)
		{
			return [result];
		}
		return [];
	}

	private static ITextDocument GetDocument(ITextBuffer buffer)
	{
		buffer.Properties.TryGetProperty<ITextDocument>(typeof(ITextDocument), out var document);
		return document;
	}

	private TagSpan<IErrorTag> GetErrorTag(NormalizedSnapshotSpanCollection spans)
	{
		if (_tagSpan is null)
		{
			if (_error is { LineNumber: not null } error)
			{
				var line = error.LineNumber.Value - 1;
				var col = (error.LinePosition ?? 1) - 1;

				if ((line < 0) || (line >= _buffer.CurrentSnapshot.LineCount) || (col < 0))
				{
					return null;
				}

				var snapshotline = _buffer.CurrentSnapshot.GetLineFromLineNumber(line);

				if ((snapshotline.Start.Position + col) >= snapshotline.Snapshot.Length)
				{
					return null;
				}

				var start = snapshotline.Start + col;
				var startSpan = new SnapshotSpan(start, start + 1);
				var span = _navigator.GetSpanOfFirstChild(startSpan);
				var tag = new ErrorTag(PredefinedErrorTypeNames.CompilerError, error.Message);

				if (!spans.IntersectsWith(span))
				{
					return null;
				}

				_tagSpan = new(span, tag);
			}
		}
		return _tagSpan;
	}

	private static Project GetProject(string fileName)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		if (string.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName))
		{
			return null;
		}

		var dte2 = (DTE2) Package.GetGlobalService(typeof(SDTE));
		var projItem = dte2?.Solution.FindProjectItem(fileName);
		return projItem?.ContainingProject;
	}

	private void HandleErrorChanged(object sender, EventArgs e)
	{
		var error = _process.Error;
		_tagSpan = null;
		if (error is not null)
		{
			_sink?.AddEntries([new XamlErrorTableEntry(_projectName, _path, error)], true);
		}
		else
		{
			_sink?.RemoveAllEntries();
		}
		RaiseTagsChanged(error);
	}

	private void RaiseTagsChanged(ExceptionDetails error)
	{
		_error = error;
		if (TagsChanged is { } tagsChanged)
		{
			var textSnapshot = _buffer.CurrentSnapshot;
			tagsChanged(this, new SnapshotSpanEventArgs(new SnapshotSpan(textSnapshot, 0, textSnapshot.Length)));
		}
	}

	IDisposable ITableDataSource.Subscribe(ITableDataSink sink)
	{
		_sink = sink;
		if (_error is { } error)
		{
			_sink?.AddEntries([new XamlErrorTableEntry(_projectName, _path, error)]);
		}
		else
		{
			_sink?.RemoveAllEntries();
		}
		return null;
	}

	#endregion

	#region Events

	public event EventHandler Disposed;
	public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

	#endregion
}