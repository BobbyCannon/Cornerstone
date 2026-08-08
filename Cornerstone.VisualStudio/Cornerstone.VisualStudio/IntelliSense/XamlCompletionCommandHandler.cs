#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Cornerstone.VisualStudio.Core.Completion;
using Cornerstone.VisualStudio.Core.Parsing;
using Cornerstone.VisualStudio.Models;
using EnvDTE;
using EnvDTE80;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using IServiceProvider = System.IServiceProvider;
using TextDocument = EnvDTE.TextDocument;

#endregion

namespace Cornerstone.VisualStudio.IntelliSense;

/// <summary>
/// Handles key presses for the Avalonia XAML intellisense completion.
/// </summary>
/// <remarks>
/// Adds a command handler to text views and listens for keypresses which should cause a
/// completion to be opened or comitted.
/// Yes, this is horrible, but it's apparently the official way to do this. Eurgh.
/// </remarks>
internal class XamlCompletionCommandHandler : IOleCommandTarget
{
	#region Fields

	private readonly ICompletionBroker _completionBroker;
	private readonly CompletionEngine _engine;
	private readonly IOleCommandTarget _nextCommandHandler;
	private readonly IServiceProvider _serviceProvider;
	private ICompletionSession _session;
	private readonly ITextView _textView;

	#endregion

	#region Constructors

	public XamlCompletionCommandHandler(
		IServiceProvider serviceProvider,
		ICompletionBroker completionBroker,
		ITextView textView,
		IVsTextView textViewAdapter,
		CompletionEngine completionEngine)
	{
		_serviceProvider = serviceProvider;
		_completionBroker = completionBroker;
		_textView = textView;
		_engine = completionEngine;

		// Add ourselves as a command to the text view.
		textViewAdapter.AddCommandFilter(this, out _nextCommandHandler);
	}

	#endregion

	#region Methods

	public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		// If we're in an automation function, move to the next command.
		if (VsShellUtilities.IsInAutomationFunction(_serviceProvider))
		{
			return _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
		}

		if (TryGetChar(ref pguidCmdGroup, nCmdID, pvaIn, out var c))
		{
			if (HandleSessionCompletion(c))
			{
				return VSConstants.S_OK;
			}

			if ((_session == null) && ((c == '\'') || (c == '"')))
			{
				// If a completion session isn't active, and we type a quote, check
				// if a quote already exists at the position & just move the cursor
				// so we don't get a double quote
				// If a completion session is active, that's handled there
				var cursorPos = _textView.Caret.Position.BufferPosition;
				var nextChar = _textView.TextSnapshot.GetText(cursorPos, 1)[0];
				if (nextChar == c)
				{
					_textView.Caret.MoveTo(cursorPos + 1);
					return VSConstants.S_OK;
				}
			}

			var result = _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

			if (HandleSessionStart(c))
			{
				return VSConstants.S_OK;
			}

			if (HandleSessionUpdate())
			{
				return VSConstants.S_OK;
			}

			return result;
		}

		return _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
	}

	/// <summary>
	/// Re-apply <paramref name="originalLine"/>'s leading whitespace onto the tracked line.
	/// Used after Enter/Tab completion commit (XML editor may smart-indent the line then).
	/// </summary>
	private void RestoreLineLeadingWhitespace(
		ITrackingPoint lineStartTracker,
		string originalLine,
		ITrackingPoint caretTracker = null)
	{
		try
		{
			var newSnapshot = _textView.TextSnapshot;
			var lineStart = lineStartTracker.GetPosition(newSnapshot);
			var newLine = newSnapshot.GetLineFromPosition(lineStart);
			var fixedLine = CompletionEngine.PreserveLineLeadingWhitespace(originalLine, newLine.GetText());
			if (!string.Equals(fixedLine, newLine.GetText(), StringComparison.Ordinal))
			{
				using (XamlTextManipulatorRegistrar.Suppress())
				using (var edit = _textView.TextBuffer.CreateEdit())
				{
					edit.Replace(newLine.Extent, fixedLine);
					newSnapshot = edit.Apply() ?? newSnapshot;
				}

				if (caretTracker != null)
				{
					var caretPos = caretTracker.GetPosition(newSnapshot);
					caretPos = Math.Max(0, Math.Min(caretPos, newSnapshot.Length));
					_textView.Caret.MoveTo(new SnapshotPoint(newSnapshot, caretPos));
				}
			}
		}
		catch
		{
			// best-effort
		}
	}

	/// <summary>
	/// Smart-indent sometimes runs after our Enter handler returns. Re-pin indent on the next UI turn(s).
	/// </summary>
	private void ScheduleLineIndentRestore(
		ITrackingPoint lineStartTracker,
		string originalLine,
		ITrackingPoint caretTracker = null)
	{
		RestoreLineIndentAfterCommitAsync(lineStartTracker, originalLine, caretTracker).FireAndForget();
	}

	private async Task RestoreLineIndentAfterCommitAsync(
		ITrackingPoint lineStartTracker,
		string originalLine,
		ITrackingPoint caretTracker)
	{
		try
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			await Task.Yield();
			RestoreLineLeadingWhitespace(lineStartTracker, originalLine, caretTracker);
			// One more pass after language service post-processing on Enter.
			await Task.Delay(30);
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			RestoreLineLeadingWhitespace(lineStartTracker, originalLine, caretTracker);
		}
		catch
		{
			// ignore
		}
	}

	public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		return _nextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
	}

	private async Task GenerateEventHandlerAsync(string controlType, string eventName, string generatedMethodName)
	{
		var currentScheduler = TaskScheduler.FromCurrentSynchronizationContext();
		try
		{
			var componentModel = (IComponentModel) Package.GetGlobalService(typeof(SComponentModel));
			var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
			var workspace = componentModel.GetService<VisualStudioWorkspace>();

			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			var activeDocument = dte.ActiveDocument;
			var activeDocumentName = $"{activeDocument.Name}.cs";
			if (activeDocument.ProjectItem?.ContainingProject?.UniqueName is { } uniqueName)
			{
				var currentDocumentCodeBehind = workspace.CurrentSolution.Projects
					.FirstOrDefault(x => x.FilePath?.EndsWith(uniqueName) == true)
					.Documents
					.FirstOrDefault(x => string.Equals(x.Name, activeDocumentName, StringComparison.OrdinalIgnoreCase));

				if (currentDocumentCodeBehind is null)
				{
					return;
				}

				var compilation = await currentDocumentCodeBehind.Project.GetCompilationAsync();
				var root = await currentDocumentCodeBehind.GetSyntaxRootAsync();
				var codeBehindClass = root.DescendantNodes()
					.FirstOrDefault(x => x.IsKind(SyntaxKind.ClassDeclaration)) as ClassDeclarationSyntax;

				await TaskScheduler.Default;

				var currentEvent = GetAllEvents(compilation.References.Select(compilation.GetAssemblyOrModuleSymbol)
						.OfType<IAssemblySymbol>().Select(a => a.GetTypeByMetadataName(controlType))
						.FirstOrDefault(x => x != null))
					.FirstOrDefault(x => x.Name == eventName) as IEventSymbol;
				var parameters = (currentEvent.Type as INamedTypeSymbol).DelegateInvokeMethod.Parameters;
				var parameterNames = new string[parameters.Length];
				var parameterTypes = new string[parameters.Length];
				for (var i = 0; i < parameters.Length; i++)
				{
					parameterNames[i] = parameters[i].MetadataName;
					parameterTypes[i] = parameters[i].Type.ToString();
				}
				var methodToInsert = GetMethodDeclarationSyntax("void", generatedMethodName, parameterTypes, parameterNames);
				var duplicatingMethodIds = new List<int>();
				foreach (MethodDeclarationSyntax item in codeBehindClass.DescendantNodes().Where(x => x.IsKind(SyntaxKind.MethodDeclaration)))
				{
					if (item.ReturnType is PredefinedTypeSyntax predefinedTypeSyntax &&
						predefinedTypeSyntax.Keyword.IsKind(SyntaxKind.VoidKeyword))
					{
						var itemParameters = item.ParameterList.Parameters.Select(x => x.Type.ToString()).ToArray();
						var methodToInsertParameters = methodToInsert.ParameterList.Parameters.Select(x => x.Type.ToString()).ToArray();
						if (itemParameters.Length == methodToInsertParameters.Length)
						{
							var sameMethods = true;
							for (var i = 0; i < itemParameters.Length; i++)
							{
								if (itemParameters[i] != methodToInsertParameters[i])
								{
									sameMethods = false;
									break;
								}
							}

							if (sameMethods)
							{
								var methodNameParts = item.Identifier.Text.Split('_');
								if ((methodNameParts.Length == 3) && int.TryParse(methodNameParts.Last(), out var methodId))
								{
									duplicatingMethodIds.Add(methodId);
								}
								else
								{
									duplicatingMethodIds.Add(0);
								}
							}
						}
					}
				}

				if (duplicatingMethodIds.Count > 0)
				{
					methodToInsert = methodToInsert.WithIdentifier(SyntaxFactory.Identifier(generatedMethodName + $"_{duplicatingMethodIds.Max() + 1}"));
				}
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				var newMethodDeclaration = codeBehindClass.AddMembers(methodToInsert);
				var newRoot = root.ReplaceNode(codeBehindClass, newMethodDeclaration);
				newRoot = Formatter.Format(newRoot, Formatter.Annotation, workspace);
				workspace.TryApplyChanges(currentDocumentCodeBehind.WithSyntaxRoot(newRoot).Project.Solution);

				// Hack to add method id to xaml file because i can't find a way to generate it from completions
				// Apply these changes after adding method because otherwise workspace will fail to add method
				if (duplicatingMethodIds.Count > 0)
				{
					var textDocument = dte.ActiveDocument.Object() as TextDocument;
					var editPoint = textDocument.CreateEditPoint();
					editPoint.MoveToAbsoluteOffset(textDocument.Selection.ActivePoint.AbsoluteCharOffset);
					editPoint.Insert($"_{duplicatingMethodIds.Max() + 1}");
				}
			}
		}
		finally
		{
			if (currentScheduler is not null && (currentScheduler.Id != TaskScheduler.FromCurrentSynchronizationContext()?.Id))
			{
				await currentScheduler;
			}
		}
	}

	private static IEnumerable<ISymbol> GetAllEvents(INamedTypeSymbol t)
	{
		foreach (var p in t.GetMembers().Where(x => x.Kind == SymbolKind.Event))
		{
			yield return p;
		}
		if (t.BaseType != null)
		{
			foreach (var p in GetAllEvents(t.BaseType))
			{
				yield return p;
			}
		}
	}

	private MethodDeclarationSyntax GetMethodDeclarationSyntax(string returnTypeName, string methodName, string[] parameterTypes, string[] paramterNames)
	{
		var parameterList = SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(GetParametersList(parameterTypes, paramterNames)));
		return SyntaxFactory.MethodDeclaration(SyntaxFactory.List<AttributeListSyntax>(),
			SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)),
			SyntaxFactory.ParseTypeName(returnTypeName),
			null,
			SyntaxFactory.Identifier(methodName),
			null,
			parameterList,
			SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
			SyntaxFactory.Block(),
			SyntaxFactory.Token(SyntaxKind.None)).WithAdditionalAnnotations(Formatter.Annotation);
	}

	private IEnumerable<ParameterSyntax> GetParametersList(string[] parameterTypes, string[] parameterNames)
	{
		for (var i = 0; i < parameterTypes.Length; i++)
		{
			yield return SyntaxFactory.Parameter(SyntaxFactory.List<AttributeListSyntax>(),
				SyntaxFactory.TokenList(),
				SyntaxFactory.ParseTypeName(parameterTypes[i]),
				SyntaxFactory.Identifier(parameterNames[i]),
				null);
		}
	}

	private bool HandleSessionCompletion(char c)
	{
		var line = _textView.GetTextViewLineContainingBufferPosition(
			_textView.Caret.Position.BufferPosition);
		var start = line.Start;
		var end = Math.Min(line.End, _textView.Caret.Position.BufferPosition);

		// Adding a xmlns is special-cased here because we don't want '.' triggering
		// a completion, which can complete on the wrong value
		// So we only trigger on ' ' or '\t', and swallow that so it doesn't get 
		// inserted into the text buffer
		var session = _session;
		if (session is not null && !session.IsDismissed)
		{
			var text = line.Snapshot.GetText(start, end - start);

			if (text.Contains("xmlns"))
			{
				if (char.IsWhiteSpace(c))
				{
					session.Commit();
					return true;
				}
				if (c == ':')
				{
					session.Dismiss();
				}

				return false;
			}
		}

		// Per UWP designer, the following keys can commit a completion session
		// in the remainder of the document - but only if a completion option
		// is selected
		// ' ' (space, or tab) 
		// '\'' (single quote)
		// '"'
		// '='
		// '>'
		// '.'

		// Also adding '#' for Selectors

		if (char.IsWhiteSpace(c)
			|| (c == '\'') || (c == '"') || (c == '=') || (c == '>') || (c == '.')
			|| (c == '#') || (c == ')') || (c == ']'))
		{
			// Prefer our tracked session; fall back to any live Avalonia session on the broker
			// (Commit/Dismiss can clear _session while a popup is still up).
			session = GetActiveAvaloniaSession() ?? session;

			if ((session != null) && !session.IsDismissed)
			{
				// Prefer Avalonia set; ensure something is selected (Filter can clear IsSelected).
				var completionSet = GetAvaloniaCompletionSet(session) ?? session.SelectedCompletionSet;
				if (completionSet != null && !completionSet.SelectionStatus.IsSelected &&
					completionSet.Completions.Count > 0)
				{
					completionSet.SelectionStatus = new CompletionSelectionStatus(
						completionSet.Completions[0], true, false);
				}

				var canCommit = completionSet?.SelectionStatus.IsSelected == true;
				if (canCommit)
				{
					var selected = completionSet.SelectionStatus.Completion as XamlCompletion;

					// Capture indent before any buffer edit — Enter commit is when smart-indent
					// tends to add a leading tab.
					var preCommitSnapshot = _textView.TextSnapshot;
					var preCommitCaret = _textView.Caret.Position.BufferPosition.Position;
					var preCommitLine = preCommitSnapshot.GetLineFromPosition(preCommitCaret);
					var preCommitLineText = preCommitLine.GetText();
					var preCommitLineStart = preCommitSnapshot.CreateTrackingPoint(
						preCommitLine.Start.Position, PointTrackingMode.Negative);

					// Always prefer manual replace for Enter/Tab so VS never sees the commit key.
					var committed = TryApplyCompletionManually(completionSet, selected);
					if (!committed && selected != null)
					{
						// Last resort: still avoid session.Commit() for Enter/Tab.
						if (c is '\n' or '\t')
						{
							DismissSession(session);
							var failCaret = preCommitSnapshot.CreateTrackingPoint(
								preCommitCaret, PointTrackingMode.Positive);
							RestoreLineLeadingWhitespace(preCommitLineStart, preCommitLineText, failCaret);
							ScheduleLineIndentRestore(preCommitLineStart, preCommitLineText, failCaret);
							return true;
						}

						using (XamlTextManipulatorRegistrar.Suppress())
						{
							session.Commit();
						}
					}
					else if (committed)
					{
						DismissSession(session);
					}

					// Enter/Tab must never fall through to VS (newline + smart-indent).
					// Re-pin indent on next UI turns; track current caret so it stays in the tag gap.
					if (c is '\n' or '\t')
					{
						var caretNow = _textView.Caret.Position.BufferPosition;
						var caretTrack = caretNow.Snapshot.CreateTrackingPoint(
							caretNow.Position, PointTrackingMode.Positive);
						RestoreLineLeadingWhitespace(preCommitLineStart, preCommitLineText, caretTrack);
						ScheduleLineIndentRestore(preCommitLineStart, preCommitLineText, caretTrack);
						return true;
					}

					// Space after element completion: swallow so we don't get "TextBlock /> " + indent.
					// User can type space themselves on the next keypress if needed.
					if ((c == ' ') && (selected?.Kind == CompletionKind.Class) &&
						(selected.InsertionText?.EndsWith("/>", StringComparison.Ordinal) == true))
					{
						return true;
					}

					if (selected?.DeleteTextOffset is int rof && !committed)
					{
						// Only when falling back to session.Commit() — manual path already applied offsets.
						var bufferPos = _textView.Caret.Position.BufferPosition;
						var newCursorPos = bufferPos.Add(rof);
						SnapshotSpan deleteSpan = newCursorPos < bufferPos
							? new(newCursorPos, -rof)
							: new(bufferPos, rof);
						_textView.TextBuffer.Delete(deleteSpan);
					}

					if (selected?.CursorOffset > 0 && !committed)
					{
						var cursorPos = _textView.Caret.Position.BufferPosition;
						var newCursorPos = cursorPos - selected.CursorOffset;
						_textView.Caret.MoveTo(newCursorPos);
					}

					// special-cased avoid TriggerCompletion
					if (selected?.InsertionText == "xmlns:")
					{
						return true;
					}

					// Re-parse after commit using current caret (pre-commit line end is stale).
					var caretAfter = _textView.Caret.Position.BufferPosition;
					var parser = XmlParser.Parse(
						_textView.TextSnapshot.GetText().AsMemory(), 0, caretAfter.Position);
					var state = parser.State;

					var skip = c != '>';
					if ((state == XmlParser.ParserState.StartElement) &&
						((c == '.') || (c == ' ')))
					{
						// Don't swallow the '.' or ' ' if this is an Xml element, like
						// Window.Resources. However do swallow tab
						skip = false;
					}

					// After self-closing element insert, never re-open completion on the commit key.
					if (selected?.InsertionText?.EndsWith("/>", StringComparison.Ordinal) == true)
					{
						return true;
					}

					if ((state == XmlParser.ParserState.AttributeValue) ||
						(state == XmlParser.ParserState.AfterAttributeValue))
					{
						var type = _engine.Helper.LookupType(parser.TagName);
						if ((type != null) && (type.Events.FirstOrDefault(x => x.Name == parser.AttributeName) != null) &&
							selected != null)
						{
							GenerateEventHandlerAsync(type.FullName, parser.AttributeName, selected.InsertionText)
								.FireAndForget();
						}
						var isSelector = parser.AttributeName?.Equals("Selector") == true;
						if (char.IsWhiteSpace(c))
						{
							// For most xml attributes, swallow the space upon completion
							// For selector, allow it to go into the buffer
							// Also if in a markupextention
							skip = !(isSelector && (c != '\n') && (c != '\t'));

							// If we're in a markup extension, only swallow the space if the
							// completion isn't on the Markup extension
							// i.e., where | is the cursor
							// {DynamicResource -> {DynamicResource |
							// but {Binding Path= -> {Binding Path=|
							// similarly, more embedded things like RelativeSource work the same way
							// {Binding path, RelativeSource={RelativeSource -> ...={RelativeSource |
							if (parser.AttributeValue?.StartsWith("{") == true)
							{
								// If press Tab or CR in expression ignore it in completation session
								if (c is '\t' or '\n')
								{
									return true;
								}
								// To determine, we'll walk back the text from the cursor position
								// until we hit either something that isn't a character
								// If that's a {, we apply the space, otherwise we dont
								// Only using the line text (up to cursor) since xaml can't wrap
								// Also ignore ':' for namespaces or directives
								var lineStart = _textView.GetTextViewLineContainingBufferPosition(caretAfter).Start;
								var text = caretAfter.Snapshot.GetText(lineStart, caretAfter.Position - lineStart);
								for (var i = text.Length - 1; i >= 0; i--)
								{
									var lineChar = text[i];
									if (char.IsLetterOrDigit(lineChar) || (lineChar == ':'))
									{
										continue;
									}

									// any other character than [A-z,0-9,:] is a different part
									skip = lineChar != '{';
									break;
								}

								// if in a markup extension, if we skip the entered char, we won't get
								// to start a new completion session, so force start it
								// The check for '=' in the insertion text ensures we don't always get this
								// e.g., {OnPlatform Wind -> {OnPlatform Windows= [New completion session]
								// but {OnPlatform Windows=Re -> {OnPlatform Windows=Red [no new session]
								if (skip && selected?.InsertionText?.EndsWith("=") == true)
								{
									TriggerCompletion();
								}
							}
						}
						else if ((c == '\'') || (c == '"'))
						{
							// If we're accepting a completion using the quotes, and there's already one
							// in the buffer after the completion, don't insert another quote, swallow
							// it and just move the cursor
							var cursorPos = _textView.Caret.Position.BufferPosition;
							if (cursorPos.Position < cursorPos.Snapshot.Length)
							{
								var nextChar = cursorPos.Snapshot.GetText(cursorPos, 1)[0];
								if (nextChar == c)
								{
									skip = true;
									_textView.Caret.MoveTo(cursorPos + 1);
								}
							}
						}
						else
						{
							skip = false;
						}

						var lastInsertionChar = (selected?.InsertionText?.Length ?? 0) > 0
							? selected.InsertionText[selected.InsertionText.Length - 1]
							: '\0';

						// Cases like {Binding Path= result in {Binding Path==
						// as the completion includes the '=', if the entered char
						// is the same as the last char here, swallow the entered char
						if (!skip && (lastInsertionChar == c))
						{
							skip = true;

							// Specifically for markup extensions, make sure '=' triggers
							// a new completion session when entered, but only if we're
							// skipping the char entered
							if (c == '=')
							{
								TriggerCompletion();
							}
						}
						else if (isSelector && lastInsertionChar is '=' or '.')
						{
							// Trigger Selector property Value Completation
							if (c is not '=' or '.')
							{
								TriggerCompletion();
							}
						}
					}
					else if ((state != XmlParser.ParserState.StartElement) ||
						(selected?.TriggerCompletion == true))
					{
						TriggerCompletion();
					}

					return skip;
				}

				// Session open but nothing to commit (empty set / no selection): dismiss.
				// Swallow Enter/Tab only so VS does not insert a newline while the popup was up.
				DismissSession(session);
				return c is '\n' or '\t';
			}

			// No active completion session — never swallow Enter/Tab (normal editing).
			return false;
		}
		if ((c == ':') && (session != null) && !session.IsDismissed)
		{
			var parser = XmlParser.Parse(_textView.TextSnapshot.GetText().AsMemory(), 0, end);
			var state = parser.State;

			if ((state == XmlParser.ParserState.AttributeValue) &&
				(parser.AttributeName?.Equals("Selector") == true))
			{
				// Force new session to start to suggest pseudoclasses
				session.Dismiss();
				return false;
			}
		}
		else if ((c == '(') && (session?.IsDismissed == false))
		{
			var parser = XmlParser.Parse(_textView.TextSnapshot.GetText().AsMemory(), 0, end);
			var state = parser.State;
			if (((state == XmlParser.ParserState.AttributeValue) || (state == XmlParser.ParserState.AfterAttributeValue))
				&& (parser.AttributeName?.Equals("Selector") == true))
			{
				session.Dismiss();
				return false;
			}
		}
		else if ((c == '{') && (session != null) && !session.IsDismissed)
		{
			var parser = XmlParser.Parse(_textView.TextSnapshot.GetText().AsMemory(), 0, end);
			var state = parser.State;

			if (state == XmlParser.ParserState.AttributeValue)
			{
				// For something like Brushes, restart the completion session if we want
				// a markup extension
				session.Dismiss();
				return false;
			}
		}
		else if ((c == ',') && (session != null) && !session.IsDismissed)
		{
			// Typing the comma in a markup extension should trigger a new completion session
			var text = line.Snapshot.GetText(start, end - start);
			for (var i = text.Length - 1; i >= 0; i--)
			{
				if (text[i] == '{')
				{
					session.Dismiss();
					return false;
				}
			}
		}

		return false;
	}

	private bool HandleSessionStart(char c)
	{
		// If the pressed key is a key that can start a completion session.
		if (CompletionEngine.ShouldTriggerCompletionListOn(c) || (c == '\a'))
		{
			var session = _session;
			if ((session == null) || session.IsDismissed)
			{
				if (TriggerCompletion() && (c != '<') && (c != '.') && (c != ' ') && (c != '[') && (c != '(') && (c != '|') && (c != '#') && (c != '/'))
				{
					session?.Filter();
				}

				return true;
			}
		}
		else if (c == ',')
		{
			var session = _session;
			if (session is null || session.IsDismissed)
			{
				if (TriggerCompletion())
				{
					session?.Filter();
				}
				return true;
			}
		}
		return false;
	}

	private bool HandleSessionUpdate()
	{
		if (_session is { } session && !session.IsDismissed)
		{
			// Prefer Filter over Recalculate — Recalculate has hit
			// ShimCompletionController.RecalculateSession NREs in VS.
			session.Filter();

			// After Filter, re-select best match for ApplicableTo text so Enter has IsSelected.
			var set = GetAvaloniaCompletionSet(session);
			if (set is { Completions.Count: > 0 })
			{
				var filterText = set.ApplicableTo?.GetText(set.ApplicableTo.TextBuffer.CurrentSnapshot) ?? "";
				XamlCompletion best = null;
				foreach (var c in set.Completions)
				{
					if (c is XamlCompletion xc &&
						(xc.DisplayText.StartsWith(filterText, StringComparison.OrdinalIgnoreCase) ||
							xc.InsertionText?.StartsWith(filterText, StringComparison.OrdinalIgnoreCase) == true))
					{
						best = xc;
						break;
					}
				}

				best ??= set.Completions[0] as XamlCompletion;
				if (best != null)
				{
					set.SelectionStatus = new CompletionSelectionStatus(best, true, false);
				}
			}

			return true;
		}
		return false;
	}

	private static Microsoft.VisualStudio.Language.Intellisense.CompletionSet GetAvaloniaCompletionSet(
		ICompletionSession session)
	{
		if (session?.CompletionSets == null)
		{
			return null;
		}

		foreach (var set in session.CompletionSets)
		{
			if (string.Equals(set.Moniker, "Avalonia", StringComparison.Ordinal))
			{
				return set;
			}
		}

		return null;
	}

	private ICompletionSession GetActiveAvaloniaSession()
	{
		if (_session is { IsDismissed: false } tracked)
		{
			return tracked;
		}

		foreach (var s in _completionBroker.GetSessions(_textView))
		{
			if (!s.IsDismissed && (GetAvaloniaCompletionSet(s) != null))
			{
				return s;
			}
		}

		return null;
	}

	/// <summary>
	/// Replaces the typed filter span with the selected insertion text and places the caret.
	/// Uses the completion engine's StartPosition at the caret (not a stale ApplicableTo span)
	/// and restores line indent if the XML editor smart-indents on the edit.
	/// </summary>
	private bool TryApplyCompletionManually(
		Microsoft.VisualStudio.Language.Intellisense.CompletionSet completionSet,
		XamlCompletion selected)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		if (selected == null)
		{
			return false;
		}

		var insertText = selected.InsertionText;
		if (insertText is null)
		{
			return false;
		}

		var snapshot = _textView.TextSnapshot;
		var caretPoint = _textView.Caret.Position.BufferPosition;
		if (caretPoint.Snapshot != snapshot)
		{
			caretPoint = caretPoint.TranslateTo(snapshot, PointTrackingMode.Positive);
		}

		var caret = caretPoint.Position;
		var fullText = snapshot.GetText();

		// Prefer a fresh engine span at the caret — ApplicableTo often goes stale after
		// typing (e.g. opened on '<' then typed TextB), which leaves partial tags behind.
		if (!TryGetEngineReplaceSpan(fullText, caret, insertText, selected, out var replaceStart, out var replaceLength) &&
			!TryGetApplicableToReplaceSpan(completionSet, snapshot, out replaceStart, out replaceLength))
		{
			return false;
		}

		if ((replaceLength < 0) || (replaceLength > 200) ||
			(replaceStart < 0) || (replaceStart + replaceLength > snapshot.Length))
		{
			return false;
		}

		var replaceSpan = new SnapshotSpan(snapshot, replaceStart, replaceLength);
		var line = snapshot.GetLineFromPosition(replaceStart);
		var originalLineContent = line.GetText();
		var lineStartTracker = snapshot.CreateTrackingPoint(line.Start.Position, PointTrackingMode.Negative);
		// Negative: stay at the *start* of the inserted text after replace.
		// Positive would jump to *after* the insert; then adding insertText.Length again
		// lands the caret inside the following tag (e.g. mid </UserControl> → </U>serControl>).
		var insertStartTracker = snapshot.CreateTrackingPoint(replaceStart, PointTrackingMode.Negative);

		// Suppress tag sync while applying the completion text.
		ITextSnapshot newSnapshot;
		using (XamlTextManipulatorRegistrar.Suppress())
		using (var edit = _textView.TextBuffer.CreateEdit())
		{
			edit.Replace(replaceSpan, insertText);
			newSnapshot = edit.Apply();
		}

		if (newSnapshot == null)
		{
			return false;
		}

		// Caret via shared pure math (same as unit tests).
		newSnapshot = _textView.TextSnapshot;
		var insertStart = insertStartTracker.GetPosition(newSnapshot);
		var recommended = selected.CaretIndexInInsert;
		if (recommended is null && (selected.CursorOffset > 0))
		{
			recommended = insertText.Length - selected.CursorOffset;
		}

		var caretPos = CompletionCaretPlacement.GetCaretAfterReplace(
			insertStart, insertText, recommended);
		caretPos = Math.Max(0, Math.Min(caretPos, newSnapshot.Length));

		// Track caret through indent restore so deferred/immediate pin does not shift it.
		var caretTracker = newSnapshot.CreateTrackingPoint(caretPos, PointTrackingMode.Positive);
		RestoreLineLeadingWhitespace(lineStartTracker, originalLineContent, caretTracker);
		newSnapshot = _textView.TextSnapshot;
		caretPos = caretTracker.GetPosition(newSnapshot);
		caretPos = Math.Max(0, Math.Min(caretPos, newSnapshot.Length));
		_textView.Caret.MoveTo(new SnapshotPoint(newSnapshot, caretPos));
		return true;
	}

	/// <summary>
	/// Computes replace [start, caret) from a fresh engine parse at the caret.
	/// </summary>
	private bool TryGetEngineReplaceSpan(
		string fullText,
		int caret,
		string insertText,
		XamlCompletion selected,
		out int replaceStart,
		out int replaceLength)
	{
		replaceStart = 0;
		replaceLength = 0;

		if (!_textView.TextBuffer.Properties.TryGetProperty(
				typeof(XamlBufferMetadata), out XamlBufferMetadata metadata) ||
			metadata?.CompletionMetadata == null)
		{
			return false;
		}

		_textView.TextBuffer.Properties.TryGetProperty("AssemblyName", out string assemblyName);

		Core.Completion.CompletionSet engineSet;
		try
		{
			engineSet = _engine.GetCompletions(
				metadata.CompletionMetadata, fullText, caret, assemblyName);
		}
		catch
		{
			return false;
		}

		if (engineSet == null)
		{
			return false;
		}

		var (start, length) = CompletionEngine.GetApplicableSpan(engineSet.StartPosition, caret);
		if ((length < 0) || (start + length > fullText.Length))
		{
			return false;
		}

		var filter = length == 0 ? string.Empty : fullText.Substring(start, length);

		// Element-name filter: must look like a partial name after '<' (or empty right after '<').
		if (length > 0)
		{
			if (!IsPlausibleCompletionFilter(filter))
			{
				return false;
			}

			// Filter should relate to what we're inserting / displaying.
			if (!insertText.StartsWith(filter, StringComparison.OrdinalIgnoreCase) &&
				!selected.DisplayText.StartsWith(filter, StringComparison.OrdinalIgnoreCase) &&
				(selected.DisplayText.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0))
			{
				// Still allow if engine start is clearly right after '<' (element completion).
				if ((start == 0) || (fullText[start - 1] != '<'))
				{
					return false;
				}
			}
		}

		replaceStart = start;
		replaceLength = length;
		return true;
	}

	private static bool TryGetApplicableToReplaceSpan(
		Microsoft.VisualStudio.Language.Intellisense.CompletionSet completionSet,
		ITextSnapshot snapshot,
		out int replaceStart,
		out int replaceLength)
	{
		replaceStart = 0;
		replaceLength = 0;
		if (completionSet?.ApplicableTo == null)
		{
			return false;
		}

		try
		{
			var span = completionSet.ApplicableTo.GetSpan(snapshot);
			replaceStart = span.Start.Position;
			replaceLength = span.Length;
			return replaceLength <= 200;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPlausibleCompletionFilter(string filter)
	{
		if (string.IsNullOrEmpty(filter) || filter.Length > 128)
		{
			return false;
		}

		foreach (var ch in filter)
		{
			if (char.IsLetterOrDigit(ch) || (ch == ':') || (ch == '.') || (ch == '_') ||
				(ch == '-') || (ch == '/') || (ch == '#'))
			{
				continue;
			}

			return false;
		}

		return true;
	}

	private void DismissSession(ICompletionSession session)
	{
		if (session == null)
		{
			return;
		}

		try
		{
			if (!session.IsDismissed)
			{
				session.Dismiss();
			}
		}
		catch
		{
			// ignore
		}

		if (ReferenceEquals(_session, session))
		{
			_session = null;
		}
	}

	private void SessionDismissed(object sender, EventArgs e)
	{
		var session = _session;
		_session = null;
		if (session != null)
		{
			session.Dismissed -= SessionDismissed;
		}
	}

	private bool TriggerCompletion()
	{
		// The caret must be in a non-projection location.
		var caretPoint = _textView.Caret.Position.Point.GetPoint(
			x => !x.ContentType.IsOfType("projection"),
			PositionAffinity.Predecessor);

		if (!caretPoint.HasValue)
		{
			return false;
		}

		// When adding an xmlns definition, we were getting 2 intellisense popups because (I think)
		// the VS XML intellisense handler was popping one up and then we are creating our own session
		// here. It turns out one of the completionsets though is an Avalonia one, so if a session already
		// exists and one of the CompletionSets is from Avalonia, use that session instead of creating
		// a new one - and we won't get the double popup
		ICompletionSession existingSession = null;
		var sessions = _completionBroker.GetSessions(_textView);
		if (sessions.Count > 0)
		{
			for (var i = sessions.Count - 1; i >= 0; i--)
			{
				if (sessions[i].CompletionSets.Count == 0)
				{
					sessions[i].Dismiss();
				}

				var sets = sessions[i].CompletionSets;

				for (var j = sets.Count - 1; j >= 0; j--)
				{
					if (sets[j].Moniker.Equals("Avalonia"))
					{
						existingSession = sessions[i];
						break;
					}
				}

				if (existingSession != null)
				{
					break;
				}
			}
		}

		var session = existingSession ?? _completionBroker.CreateCompletionSession(
			_textView,
			caretPoint?.Snapshot.CreateTrackingPoint(caretPoint.Value.Position, PointTrackingMode.Positive),
			true);

		// Subscribe to the Dismissed event on the session.
		session.Dismissed += SessionDismissed;
		_session = session;
		session.Start();
		return true;
	}

	private static bool TryGetChar(ref Guid pguidCmdGroup, uint nCmdID, IntPtr pvaIn, out char c)
	{
		c = '\0';

		if (pguidCmdGroup == VSConstants.VSStd2K)
		{
			switch ((VSConstants.VSStd2KCmdID) nCmdID)
			{
				case VSConstants.VSStd2KCmdID.TYPECHAR:
					c = (char) (ushort) Marshal.GetObjectForNativeVariant(pvaIn);
					break;
				case VSConstants.VSStd2KCmdID.RETURN:
					c = '\n';
					break;
				case VSConstants.VSStd2KCmdID.TAB:
					c = '\t';
					break;
				case VSConstants.VSStd2KCmdID.BACKSPACE:
				case VSConstants.VSStd2KCmdID.DELETE:
					c = '\b';
					break;
				// Translate Ctrl+Space into a '\a'.
				case VSConstants.VSStd2KCmdID.COMPLETEWORD:
					c = ' ';
					break;
			}
		}

		return c != '\0';
	}

	#endregion
}