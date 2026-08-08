#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cornerstone.VisualStudio.Core.Cleanup;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Serilog;
using Task = System.Threading.Tasks.Task;

#endregion

namespace Cornerstone.VisualStudio.Services;

/// <summary>
/// Applies <see cref="CleanupPipeline" /> to open buffers or files on disk.
/// </summary>
internal sealed class CodeCleanupService
{
	#region Fields

	private readonly AsyncPackage _package;

	#endregion

	#region Constructors

	public CodeCleanupService(AsyncPackage package)
	{
		_package = package ?? throw new ArgumentNullException(nameof(package));
	}

	#endregion

	#region Methods

	public async Task CleanActiveDocumentAsync()
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		var settings = _package.GetMefService<ICornerstoneSettings>();
		var options = settings.CreateCleanupOptions();
		var dte = (DTE) await _package.GetServiceAsync(typeof(DTE));
		if (dte?.ActiveDocument == null)
		{
			Log.Information("Cornerstone: Code Cleanup — no active document");
			await SetStatusAsync("Cornerstone: Code Cleanup — no active document");
			return;
		}

		var doc = dte.ActiveDocument;
		var path = doc.FullName;
		if (!string.IsNullOrWhiteSpace(path) && !options.MatchesExtension(path))
		{
			Log.Information("Cornerstone: Code Cleanup — skipped active document (extension filter): {Path}", path);
			await SetStatusAsync("Cornerstone: Code Cleanup — file type not in configured extensions");
			return;
		}

		// Selection: hygiene-only when a non-empty partial selection exists.
		if (doc.Selection is TextSelection selection &&
			!string.IsNullOrEmpty(selection.Text) &&
			selection.TopPoint.AbsoluteCharOffset != selection.BottomPoint.AbsoluteCharOffset)
		{
			var selected = selection.Text;
			var fullLen = 0;
			try
			{
				if (doc.Object("TextDocument") is TextDocument textDoc)
				{
					var editPoint = textDoc.StartPoint.CreateEditPoint();
					fullLen = editPoint.GetText(textDoc.EndPoint).Length;
				}
			}
			catch
			{
				// ignored
			}

			var isFullDocument = fullLen > 0 && selected.Length >= fullLen;
			if (!isFullDocument)
			{
				var result = CleanupPipeline.CleanSelection(selected, options);
				if (result.HasTextChange)
				{
					selection.Insert(result.Text);
					try
					{
						doc.Save();
					}
					catch (Exception ex)
					{
						Log.Warning(ex, "Code Cleanup: failed to save after selection cleanup");
					}
				}

				LogResult("selection", path ?? doc.Name, result);
				await SetStatusAsync(SummarizeSingle(result));
				return;
			}
		}

		if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
		{
			var batch = await CleanFilesAsync(new[] { Path.GetFullPath(path) }, options);
			await SetStatusAsync(SummarizeBatch(batch));
			return;
		}

		// Untitled / no path: edit via DTE TextDocument.
		await CleanDteDocumentAsync(doc, options);
	}

	public async Task CleanHierarchySelectionAsync()
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		var settings = _package.GetMefService<ICornerstoneSettings>();
		var options = settings.CreateCleanupOptions();
		var dte = (DTE) await _package.GetServiceAsync(typeof(DTE));
		if (dte == null)
		{
			return;
		}

		var files = HierarchyFileEnumerator.GetSelectedFilePaths(dte, options.MatchesExtension);
		if (files.Count == 0)
		{
			Log.Information("Cornerstone: Code Cleanup — no matching files in selection (extensions: {Ext})", options.FileExtensions);
			await SetStatusAsync("Cornerstone: Code Cleanup — no matching files in selection");
			return;
		}

		var batch = await CleanFilesAsync(files, options);
		await SetStatusAsync(SummarizeBatch(batch));
	}

	public async Task<bool> SelectionHasMatchingFilesAsync()
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		var settings = _package.GetMefService<ICornerstoneSettings>();
		var options = settings.CreateCleanupOptions();
		var dte = (DTE) await _package.GetServiceAsync(typeof(DTE));
		if (dte == null)
		{
			return false;
		}

		var files = HierarchyFileEnumerator.GetSelectedFilePaths(dte, options.MatchesExtension);
		return files.Count > 0;
	}

	private async Task<BatchSummary> CleanFilesAsync(IReadOnlyList<string> files, CleanupOptions options)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		var summary = new BatchSummary { Total = files.Count };
		Log.Information("Cornerstone: Code Cleanup — processing {Count} file(s)", files.Count);

		for (var i = 0; i < files.Count; i++)
		{
			var path = files[i];
			await SetStatusAsync($"Cornerstone: Code Cleanup — {i + 1}/{files.Count}…");

			try
			{
				var outcome = await CleanOneFileAsync(path, options);
				summary.Add(outcome);
				LogResult(Path.GetFileName(path), path, outcome.Result);
			}
			catch (Exception ex)
			{
				summary.Errors++;
				Log.Error(ex, "Code Cleanup failed for {Path}", path);
			}
		}

		Log.Information(
			"Cornerstone: Code Cleanup complete — changed {Changed}, unchanged {Unchanged}, skipped {Skipped}, errors {Errors} (of {Total})",
			summary.Changed,
			summary.Unchanged,
			summary.Skipped,
			summary.Errors,
			summary.Total);

		return summary;
	}

	private async Task<FileOutcome> CleanOneFileAsync(string path, CleanupOptions options)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		if (!File.Exists(path))
		{
			return FileOutcome.Skip(CleanupResult.CreateSkipped(string.Empty, "File not found."));
		}

		var info = new FileInfo(path);
		if (info.Length > options.MaxFileBytes)
		{
			return FileOutcome.Skip(CleanupResult.CreateSkipped(string.Empty, $"File exceeds size limit ({options.MaxFileBytes} bytes)."));
		}

		if (info.IsReadOnly || (info.Attributes & FileAttributes.ReadOnly) != 0)
		{
			// Still allow open editable buffers.
			if (!IsDocumentOpenEditable(path))
			{
				return FileOutcome.Skip(CleanupResult.CreateSkipped(string.Empty, "File is read-only."));
			}
		}

		// Prefer open text buffer / DTE document when present.
		var dte = (DTE) await _package.GetServiceAsync(typeof(DTE));
		Document openDoc = null;
		try
		{
			openDoc = dte?.Documents?.OfType<Document>()
				.FirstOrDefault(d =>
				{
					ThreadHelper.ThrowIfNotOnUIThread();
					try
					{
						return !string.IsNullOrEmpty(d.FullName) &&
							string.Equals(Path.GetFullPath(d.FullName), path, StringComparison.OrdinalIgnoreCase);
					}
					catch
					{
						return false;
					}
				});
		}
		catch
		{
			openDoc = null;
		}

		if (openDoc != null)
		{
			return await CleanDteDocumentAsync(openDoc, options);
		}

		// Disk path
		string original;
		Encoding encoding;
		try
		{
			var bytes = File.ReadAllBytes(path);
			encoding = DetectEncoding(bytes, out var preambleLength);
			original = encoding.GetString(bytes, preambleLength, bytes.Length - preambleLength);
		}
		catch (Exception ex)
		{
			return FileOutcome.Error(CleanupResult.CreateError(string.Empty, ex.Message));
		}

		var result = CleanupPipeline.Clean(original, options);
		if (!result.HasTextChange)
		{
			return FileOutcome.FromResult(result);
		}

		try
		{
			// DetectEncoding returns UTF8 with BOM when preamble was present.
			File.WriteAllText(path, result.Text, encoding);
		}
		catch (Exception ex)
		{
			return FileOutcome.Error(CleanupResult.CreateError(original, ex.Message));
		}

		return FileOutcome.FromResult(result);
	}

	private async Task<FileOutcome> CleanDteDocumentAsync(Document doc, CleanupOptions options)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		try
		{
			if (doc.Object("TextDocument") is not TextDocument textDoc)
			{
				return FileOutcome.Skip(CleanupResult.CreateSkipped(string.Empty, "Not a text document."));
			}

			var start = textDoc.StartPoint.CreateEditPoint();
			var original = start.GetText(textDoc.EndPoint);
			var result = CleanupPipeline.Clean(original, options);
			if (result.HasTextChange)
			{
				start.ReplaceText(textDoc.EndPoint, result.Text, (int) vsEPReplaceTextOptions.vsEPReplaceTextKeepMarkers);
				try
				{
					if (!doc.Saved)
					{
						doc.Save();
					}
					else
					{
						// Ensure write-back when VS still marks saved after edit in some hosts.
						doc.Save();
					}
				}
				catch (Exception ex)
				{
					Log.Warning(ex, "Code Cleanup: save failed for {Name}", doc.Name);
				}
			}

			return FileOutcome.FromResult(result);
		}
		catch (Exception ex)
		{
			return FileOutcome.Error(CleanupResult.CreateError(string.Empty, ex.Message));
		}
	}

	private bool IsDocumentOpenEditable(string path)
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		try
		{
			var dte = (DTE) Package.GetGlobalService(typeof(DTE));
			var doc = dte?.Documents?.OfType<Document>().FirstOrDefault(d =>
			{
				ThreadHelper.ThrowIfNotOnUIThread();
				try
				{
					return string.Equals(Path.GetFullPath(d.FullName), path, StringComparison.OrdinalIgnoreCase);
				}
				catch
				{
					return false;
				}
			});
			return doc != null && !doc.ReadOnly;
		}
		catch
		{
			return false;
		}
	}

	private static Encoding DetectEncoding(byte[] bytes, out int preambleLength)
	{
		preambleLength = 0;
		if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
		{
			preambleLength = 3;
			return new UTF8Encoding(true);
		}

		if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
		{
			preambleLength = 2;
			return Encoding.Unicode;
		}

		if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
		{
			preambleLength = 2;
			return Encoding.BigEndianUnicode;
		}

		return new UTF8Encoding(false);
	}

	private static void LogResult(string name, string path, CleanupResult result)
	{
		if (result == null)
		{
			return;
		}

		switch (result.Outcome)
		{
			case CleanupOutcome.Changed:
				Log.Information("Code Cleanup changed {Name} ({Path})", name, path);
				break;
			case CleanupOutcome.Unchanged:
				Log.Debug("Code Cleanup unchanged {Name}", name);
				break;
			case CleanupOutcome.StructuralSkipped:
				Log.Information("Code Cleanup hygiene only for {Name}: {Message}", name, result.Message);
				break;
			case CleanupOutcome.Skipped:
				Log.Information("Code Cleanup skipped {Name}: {Message}", name, result.Message);
				break;
			case CleanupOutcome.Error:
				Log.Warning("Code Cleanup error {Name}: {Message}", name, result.Message);
				break;
		}
	}

	private async Task SetStatusAsync(string text)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
		try
		{
			var statusBar = await _package.GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar;
			if (statusBar != null)
			{
				statusBar.SetText(text);
			}
		}
		catch
		{
			// ignored
		}
	}

	private static string SummarizeSingle(CleanupResult result)
	{
		if (result.HasTextChange)
		{
			return "Cornerstone: Code Cleanup — document updated";
		}

		return result.Outcome == CleanupOutcome.Error
			? "Cornerstone: Code Cleanup — error (see Output)"
			: "Cornerstone: Code Cleanup — no changes";
	}

	private static string SummarizeBatch(BatchSummary summary)
	{
		return $"Cornerstone: Code Cleanup — {summary.Changed} changed, {summary.Unchanged} unchanged, {summary.Skipped} skipped, {summary.Errors} errors";
	}

	#endregion

	#region Nested Types

	private sealed class BatchSummary
	{
		public int Changed;
		public int Errors;
		public int Skipped;
		public int Total;
		public int Unchanged;

		public void Add(FileOutcome outcome)
		{
			if (outcome == null)
			{
				Errors++;
				return;
			}

			if (outcome.IsError)
			{
				Errors++;
				return;
			}

			if (outcome.Result == null)
			{
				Skipped++;
				return;
			}

			if (outcome.Result.HasTextChange)
			{
				Changed++;
			}
			else if (outcome.Result.Outcome is CleanupOutcome.Skipped or CleanupOutcome.StructuralSkipped)
			{
				Skipped++;
			}
			else
			{
				Unchanged++;
			}
		}
	}

	private sealed class FileOutcome
	{
		public bool IsError { get; private set; }
		public CleanupResult Result { get; private set; }

		public static FileOutcome FromResult(CleanupResult result) => new() { Result = result };

		public static FileOutcome Skip(CleanupResult result) => new() { Result = result };

		public static FileOutcome Error(CleanupResult result) => new() { Result = result, IsError = true };
	}

	#endregion
}
