#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.VisualStudio.Core.Completion;
using Cornerstone.VisualStudio.Core.Parsing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

#endregion

namespace Cornerstone.VisualStudio.IntelliSense;

internal static class XamlGoToDefinitionNavigator
{
	#region Methods

	public static async Task<bool> NavigateAsync(XamlGoToDefinitionTarget target, ITextView textView)
	{
		if ((target == null) || (target.Kind == XamlGoToDefinitionKind.None))
		{
			return false;
		}

		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
		RememberNavigationPoint(textView, null);

		var buffer = textView?.TextBuffer;
		if (target.Kind == XamlGoToDefinitionKind.StyleClass)
		{
			return await GoToStyleClassAsync(textView, target).ConfigureAwait(true);
		}

		var componentModel = Package.GetGlobalService(typeof(SComponentModel)) as IComponentModel;
		var workspace = componentModel?.GetService<VisualStudioWorkspace>();
		if (workspace == null)
		{
			return false;
		}

		await TaskScheduler.Default.SwitchTo();

		switch (target.Kind)
		{
			case XamlGoToDefinitionKind.Type:
				return await GoToTypeAsync(workspace, target.TypeFullName).ConfigureAwait(true);
			case XamlGoToDefinitionKind.Member:
				if (await GoToMemberAsync(workspace, target.TypeFullName, target.MemberName).ConfigureAwait(true))
				{
					return true;
				}

				return await GoToTypeAsync(workspace, target.TypeFullName).ConfigureAwait(true);
			case XamlGoToDefinitionKind.ClassName:
				return await GoToTypeAsync(workspace, target.ClassName).ConfigureAwait(true);
			case XamlGoToDefinitionKind.MethodName:
				if (await GoToMethodAsync(workspace, buffer, target.ClassName, target.MethodName).ConfigureAwait(true))
				{
					return true;
				}

				return await GoToTypeAsync(workspace, target.ClassName).ConfigureAwait(true);
			default:
				return false;
		}
	}

	private static IVsTextBuffer GetTextBuffer(object docData)
	{
		if (docData is IVsTextBuffer buffer)
		{
			return buffer;
		}

		if (docData is IVsTextBufferProvider provider)
		{
			provider.GetTextBuffer(out var lines);
			return lines;
		}

		return null;
	}

	private static string GetBufferFilePath(ITextBuffer buffer)
	{
		if (buffer?.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document) == true)
		{
			return document.FilePath;
		}

		return null;
	}

	private static async Task<bool> GoToStyleClassAsync(ITextView textView, XamlGoToDefinitionTarget target)
	{
		if (string.IsNullOrEmpty(target.ClassName))
		{
			return false;
		}

		if (target.DocumentOffset >= 0)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			return TryMoveCaret(textView, target.DocumentOffset);
		}

		await TaskScheduler.Default.SwitchTo();
		var currentPath = GetBufferFilePath(textView?.TextBuffer);
		var caret = -1;
		if (textView != null)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			caret = textView.Caret.Position.BufferPosition.Position;
			await TaskScheduler.Default.SwitchTo();
		}

		var componentModel = Package.GetGlobalService(typeof(SComponentModel)) as IComponentModel;
		var workspace = componentModel?.GetService<VisualStudioWorkspace>();
		if (workspace == null)
		{
			return false;
		}

		foreach (var project in workspace.CurrentSolution.Projects)
		{
			foreach (var document in EnumerateXamlDocuments(project))
			{
				var text = await document.GetTextAsync().ConfigureAwait(false);
				var xml = text.ToString();
				var exclude = PathsEqual(document.FilePath, currentPath) ? caret : -1;
				var offset = StyleClassScanner.FindSelectorClassOffset(xml, target.ClassName, exclude);
				if (offset < 0)
				{
					continue;
				}

				OffsetToLineColumn(xml, offset, out var line, out var column);
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				if (TryOpenFileAtPosition(document.FilePath, line, column))
				{
					return true;
				}

				await TaskScheduler.Default.SwitchTo();
			}
		}

		return false;
	}

	private static async Task<bool> GoToMemberAsync(
		VisualStudioWorkspace workspace,
		string typeFullName,
		string memberName)
	{
		if (string.IsNullOrEmpty(typeFullName) || string.IsNullOrEmpty(memberName))
		{
			return false;
		}

		ISymbol metadataMember = null;
		Project metadataProject = null;

		foreach (var project in workspace.CurrentSolution.Projects)
		{
			var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
			var type = compilation?.GetTypeByMetadataName(typeFullName);
			var member = type?.GetMembers(memberName).FirstOrDefault(m => m.Locations.Any(l => l.IsInSource));
			if (member != null)
			{
				return await NavigateToSymbolAsync(workspace, member, project).ConfigureAwait(true);
			}

			member = type?.GetMembers(memberName).FirstOrDefault();
			if ((member != null) && (metadataMember == null))
			{
				metadataMember = member;
				metadataProject = project;
			}
		}

		if (metadataMember != null)
		{
			return await NavigateToSymbolAsync(workspace, metadataMember, metadataProject).ConfigureAwait(true);
		}

		return false;
	}

	private static async Task<bool> GoToMethodAsync(
		VisualStudioWorkspace workspace,
		ITextBuffer buffer,
		string className,
		string methodName)
	{
		if (string.IsNullOrEmpty(methodName))
		{
			return false;
		}

		string xamlPath = null;
		if (buffer?.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document) == true)
		{
			xamlPath = document.FilePath;
		}

		foreach (var project in workspace.CurrentSolution.Projects)
		{
			foreach (var doc in project.Documents)
			{
				if (!IsLikelyCodeBehind(doc.FilePath, xamlPath) &&
					!TypeNameMatches(doc, className))
				{
					continue;
				}

				var model = await doc.GetSemanticModelAsync().ConfigureAwait(false);
				if (model == null)
				{
					continue;
				}

				var tree = await doc.GetSyntaxTreeAsync().ConfigureAwait(false);
				if (tree == null)
				{
					continue;
				}

				var root = await tree.GetRootAsync().ConfigureAwait(false);
				foreach (var node in root.DescendantNodes())
				{
					var symbol = model.GetDeclaredSymbol(node);
					if ((symbol is IMethodSymbol method) &&
						string.Equals(method.Name, methodName, StringComparison.Ordinal))
					{
						return await NavigateToSymbolAsync(workspace, method, project).ConfigureAwait(true);
					}
				}
			}
		}

		return false;
	}

	private static async Task<bool> GoToTypeAsync(VisualStudioWorkspace workspace, string metadataName)
	{
		if (string.IsNullOrEmpty(metadataName))
		{
			return false;
		}

		INamedTypeSymbol sourceType = null;
		INamedTypeSymbol metadataType = null;
		Project sourceProject = null;
		Project metadataProject = null;

		foreach (var project in workspace.CurrentSolution.Projects)
		{
			var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
			var type = compilation?.GetTypeByMetadataName(metadataName);
			if (type == null)
			{
				continue;
			}

			if (type.Locations.Any(l => l.IsInSource))
			{
				sourceType = type;
				sourceProject = project;
				break;
			}

			if (metadataType == null)
			{
				metadataType = type;
				metadataProject = project;
			}
		}

		if (sourceType != null)
		{
			return await NavigateToSymbolAsync(workspace, sourceType, sourceProject).ConfigureAwait(true);
		}

		var simple = metadataName;
		var lastDot = metadataName.LastIndexOf('.');
		if (lastDot >= 0)
		{
			simple = metadataName.Substring(lastDot + 1);
		}

		foreach (var project in workspace.CurrentSolution.Projects)
		{
			var declarations = await SymbolFinder.FindDeclarationsAsync(
				project,
				simple,
				ignoreCase: true,
				SymbolFilter.Type,
				CancellationToken.None).ConfigureAwait(false);

			foreach (var symbol in declarations)
			{
				if (!symbol.Locations.Any(l => l.IsInSource))
				{
					continue;
				}

				return await NavigateToSymbolAsync(workspace, symbol, project).ConfigureAwait(true);
			}
		}

		if (metadataType != null)
		{
			return await NavigateToSymbolAsync(workspace, metadataType, metadataProject).ConfigureAwait(true);
		}

		return false;
	}

	private static bool HasSourceLocation(ISymbol symbol)
	{
		foreach (var location in symbol.Locations)
		{
			if (location.IsInSource)
			{
				return true;
			}
		}

		return false;
	}

	private static IEnumerable<TextDocument> EnumerateXamlDocuments(Project project)
	{
		foreach (var document in project.AdditionalDocuments)
		{
			if (IsXamlDocument(document.FilePath))
			{
				yield return document;
			}
		}

		foreach (var document in project.Documents)
		{
			if (IsXamlDocument(document.FilePath))
			{
				yield return document;
			}
		}
	}

	private static bool IsXamlDocument(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return false;
		}

		return path.EndsWith(".cxaml", StringComparison.OrdinalIgnoreCase) ||
			path.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase);
	}

	private static void OffsetToLineColumn(string text, int offset, out int line, out int column)
	{
		line = 0;
		column = 0;
		if (string.IsNullOrEmpty(text) || (offset <= 0))
		{
			return;
		}

		var max = Math.Min(offset, text.Length);
		var lineStart = 0;
		for (var i = 0; i < max; i++)
		{
			if (text[i] == '\n')
			{
				line++;
				lineStart = i + 1;
			}
		}

		column = max - lineStart;
	}

	private static bool PathsEqual(string left, string right)
	{
		if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
		{
			return false;
		}

		return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
	}

	public static bool RememberNavigationPoint(ITextView textView, IVsWindowFrame frame)
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		if ((textView?.Caret == null) || (textView.TextSnapshot == null))
		{
			return false;
		}

		if (frame == null)
		{
			var selection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
			if ((selection == null) ||
				ErrorHandler.Failed(selection.GetCurrentElementValue((uint) VSConstants.VSSELELEMID.SEID_WindowFrame, out var frameObject)))
			{
				return false;
			}

			frame = frameObject as IVsWindowFrame;
		}

		if (frame == null)
		{
			return false;
		}

		var shell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;
		if (shell == null)
		{
			return false;
		}

		var position = textView.Caret.Position.BufferPosition;
		var line = position.GetContainingLine();
		var data = "CXAML:" + line.LineNumber + "," + (position.Position - line.Start.Position);
		return ErrorHandler.Succeeded(shell.AddNewBFNavigationItem(frame, data, null, 0));
	}

	public static bool TryRestoreNavigationPoint(ITextView textView, string data)
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		int lineNumber;
		int column;
		if (!TryParseNavigationPoint(data, out lineNumber, out column) || (textView?.TextSnapshot == null))
		{
			return false;
		}

		if ((lineNumber < 0) || (lineNumber >= textView.TextSnapshot.LineCount))
		{
			return false;
		}

		var line = textView.TextSnapshot.GetLineFromLineNumber(lineNumber);
		var position = Math.Min(line.Start.Position + Math.Max(0, column), line.End.Position);
		var point = new SnapshotPoint(textView.TextSnapshot, position);
		textView.Caret.MoveTo(point);
		textView.ViewScroller.EnsureSpanVisible(new SnapshotSpan(point, 0), EnsureSpanVisibleOptions.AlwaysCenter);
		return true;
	}

	private static bool TryParseNavigationPoint(string data, out int lineNumber, out int column)
	{
		lineNumber = 0;
		column = 0;
		if (string.IsNullOrEmpty(data) || !data.StartsWith("CXAML:", StringComparison.Ordinal))
		{
			return false;
		}

		var parts = data.Substring(6).Split(',');
		return (parts.Length == 2) &&
			int.TryParse(parts[0], out lineNumber) &&
			int.TryParse(parts[1], out column);
	}

	private static bool TryMoveCaret(ITextView textView, int offset)
	{
		if (textView?.TextSnapshot == null)
		{
			return false;
		}

		var snapshot = textView.TextSnapshot;
		offset = Math.Max(0, Math.Min(offset, snapshot.Length));
		var point = new SnapshotPoint(snapshot, offset);
		textView.Caret.MoveTo(point);
		textView.ViewScroller.EnsureSpanVisible(new SnapshotSpan(point, 0));
		return true;
	}

	private static bool IsLikelyCodeBehind(string documentPath, string xamlPath)
	{
		if (string.IsNullOrEmpty(documentPath) || string.IsNullOrEmpty(xamlPath))
		{
			return false;
		}

		return documentPath.StartsWith(xamlPath, StringComparison.OrdinalIgnoreCase) &&
			documentPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
	}

	private static async Task<bool> NavigateToSymbolAsync(
		VisualStudioWorkspace workspace,
		ISymbol symbol,
		Project project)
	{
		var location = PickSourceLocation(symbol);
		if ((location != null) && (location.SourceTree != null) && !string.IsNullOrEmpty(location.SourceTree.FilePath))
		{
			var lineSpan = location.GetLineSpan();
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			if (TryOpenFileAtPosition(
				location.SourceTree.FilePath,
				lineSpan.StartLinePosition.Line,
				lineSpan.StartLinePosition.Character))
			{
				return true;
			}
		}

		if ((project != null) && !HasSourceLocation(symbol))
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			return await workspace.TryGoToDefinitionAsync(symbol, project, CancellationToken.None);
		}

		return false;
	}

	private static Location PickSourceLocation(ISymbol symbol)
	{
		var locations = new List<Location>();
		foreach (var location in symbol.Locations)
		{
			if (location.IsInSource && (location.SourceTree != null))
			{
				locations.Add(location);
			}
		}

		if (locations.Count == 0)
		{
			foreach (var reference in symbol.DeclaringSyntaxReferences)
			{
				var syntax = reference.GetSyntax();
				var location = syntax?.GetLocation();
				if ((location != null) && location.IsInSource && (location.SourceTree != null))
				{
					locations.Add(location);
				}
			}
		}

		if (locations.Count == 0)
		{
			return null;
		}

		var preferredPath = GoToDefinitionLocationPicker.PickPreferredPath(
			locations.Select(l => l.SourceTree.FilePath),
			symbol.Name);
		if (string.IsNullOrEmpty(preferredPath))
		{
			return locations[0];
		}

		foreach (var location in locations)
		{
			if (string.Equals(location.SourceTree.FilePath, preferredPath, StringComparison.OrdinalIgnoreCase))
			{
				return location;
			}
		}

		return locations[0];
	}

	private static bool TryOpenFileAtPosition(string filePath, int line, int column)
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		try
		{
			using (new NewDocumentStateScope(__VSNEWDOCUMENTSTATE.NDS_Permanent, VSConstants.NewDocumentStateReason.Navigation))
			{
				VsShellUtilities.OpenDocument(
					ServiceProvider.GlobalProvider,
					filePath,
					VSConstants.LOGVIEWID_Code,
					out _,
					out _,
					out var frame);
				if (frame == null)
				{
					return false;
				}

				if (ErrorHandler.Failed(frame.Show()))
				{
					return false;
				}

				if (ErrorHandler.Failed(frame.GetProperty((int) __VSFPROPID.VSFPROPID_DocData, out var docData)))
				{
					return true;
				}

				var textBuffer = GetTextBuffer(docData);
				var textManager = Package.GetGlobalService(typeof(SVsTextManager)) as IVsTextManager;
				if ((textManager == null) || (textBuffer == null))
				{
					return true;
				}

				var viewId = VSConstants.LOGVIEWID_Code;
				return ErrorHandler.Succeeded(textManager.NavigateToLineAndColumn(
					textBuffer,
					ref viewId,
					line,
					column,
					line,
					column));
			}
		}
		catch
		{
			return false;
		}
	}

	private static bool TypeNameMatches(Microsoft.CodeAnalysis.Document document, string className)
	{
		if (string.IsNullOrEmpty(className) || (document?.Name == null))
		{
			return false;
		}

		var simple = className;
		var lastDot = className.LastIndexOf('.');
		if (lastDot >= 0)
		{
			simple = className.Substring(lastDot + 1);
		}

		return document.Name.IndexOf(simple, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	#endregion
}
