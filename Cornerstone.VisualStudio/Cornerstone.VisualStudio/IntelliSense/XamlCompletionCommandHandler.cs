#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Cornerstone.VisualStudio.Core.Completion;
using Cornerstone.VisualStudio.Protocol;
using Cornerstone.VisualStudio.Services;
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
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using Serilog;
using IServiceProvider = System.IServiceProvider;
using TextDocument = EnvDTE.TextDocument;

#endregion

namespace Cornerstone.VisualStudio.IntelliSense;

/// <summary>
/// Handles key presses for the Avalonia XAML intellisense completion.
/// </summary>
/// <remarks>
/// Adds a command handler to text views and listens for keypresses which should cause a
/// completion to be opened or committed.
/// Yes, this is horrible, but it's apparently the official way to do this. Eurgh.
/// </remarks>
internal class XamlCompletionCommandHandler : IOleCommandTarget
{
	#region Fields

	private readonly ICompletionBroker _completionBroker;
	private readonly CompletionEngine _engine;
	private IOleCommandTarget _nextCommandHandler;
	private readonly IServiceProvider _serviceProvider;
	private ICompletionSession _session;
	private readonly StyleClassNameIndex _styleClassNameIndex;
	private readonly ITextUndoHistoryRegistry _textUndoHistoryRegistry;
	private readonly ITextView _textView;
	private readonly IVsTextView _textViewAdapter;
	private bool _deferringHostCompletion;
	private int _hostCompletionGeneration;
	private bool _languageHeld;

	#endregion

	#region Constructors

	public XamlCompletionCommandHandler(
		IServiceProvider serviceProvider,
		ICompletionBroker completionBroker,
		ITextView textView,
		IVsTextView textViewAdapter,
		CompletionEngine completionEngine,
		ITextUndoHistoryRegistry textUndoHistoryRegistry,
		StyleClassNameIndex styleClassNameIndex)
	{
		_serviceProvider = serviceProvider;
		_completionBroker = completionBroker;
		_textView = textView;
		_textViewAdapter = textViewAdapter;
		_engine = completionEngine;
		_textUndoHistoryRegistry = textUndoHistoryRegistry;
		_styleClassNameIndex = styleClassNameIndex;
		_deferringHostCompletion = false;
		_hostCompletionGeneration = 0;
		_languageHeld = false;

		textViewAdapter.AddCommandFilter(this, out _nextCommandHandler);
		_textView.GotAggregateFocus += OnGotAggregateFocus;
		_textView.Closed += OnTextViewClosed;
		SchedulePromoteCommandFilter();
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

		if (XamlGoToDefinitionCommand.IsCommand(pguidCmdGroup, nCmdID))
		{
			if (XamlGoToDefinitionCommand.IsHandingOff(_textView))
			{
				return _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
			}

			var group = pguidCmdGroup;
			var commandId = nCmdID;
			var options = nCmdexecopt;
			XamlGoToDefinitionCommand.Execute(_textView, _engine, () =>
			{
				_nextCommandHandler.Exec(ref group, commandId, options, IntPtr.Zero, IntPtr.Zero);
			});
			return VSConstants.S_OK;
		}

		if (IsInvokeCompletionCommand(pguidCmdGroup, nCmdID))
		{
			if (TryInvokeCompletion())
			{
				return VSConstants.S_OK;
			}

			return _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
		}

		if (TryGetChar(ref pguidCmdGroup, nCmdID, pvaIn, out var c))
		{
			if (HandleSessionCompletion(c))
			{
				return VSConstants.S_OK;
			}

			// No completion to commit. Ask the editor host for the newline. Do not
			// block this command, or a slow host freezes the shell for its timeout.
			if ((c == '\n') && BeginEnterIndent())
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
				if (cursorPos.Position < cursorPos.Snapshot.Length)
				{
					var nextChar = cursorPos.Snapshot.GetText(cursorPos, 1)[0];
					if (nextChar == c)
					{
						_textView.Caret.MoveTo(cursorPos + 1);
						// XML often auto-pairs quotes on '=' so typing '"' lands in Classes="|".
						// Skip must still open value completion (style classes, enums, …).
						var entered = ParseToCaret(_textView.Caret.Position.BufferPosition.Position);
						if ((entered.State == XmlParser.ParserState.AttributeValue) &&
							!XamlEditCompleteness.IsFreeTextAttributeValue(entered))
						{
							TryInvokeCompletion();
						}

						return VSConstants.S_OK;
					}
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

	public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		if (XamlGoToDefinitionCommand.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds) == VSConstants.S_OK)
		{
			return VSConstants.S_OK;
		}

		if ((pguidCmdGroup == VSConstants.VSStd2K) && (prgCmds != null))
		{
			var handled = false;
			for (var i = 0; i < cCmds; i++)
			{
				if (IsInvokeCompletionCommand(pguidCmdGroup, prgCmds[i].cmdID))
				{
					prgCmds[i].cmdf = (uint) (OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);
					handled = true;
				}
			}

			if (handled)
			{
				return VSConstants.S_OK;
			}
		}

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

					using (XamlTextManipulatorRegistrar.Suppress())
					{
						XmlEditorSmartIndent.SuppressUntilAfterCurrentCommand(_textView);
						session.Commit();
					}

					if (c is '\n' or '\t')
					{
						if (selected?.InsertionText?.EndsWith("\"\"", StringComparison.Ordinal) == true)
						{
							TryInvokeCompletion();
						}

						return true;
					}

					if ((c == ' ') && (selected?.Kind == CompletionKind.Class) &&
						(selected.InsertionText?.EndsWith("/>", StringComparison.Ordinal) == true))
					{
						return true;
					}

					if (selected?.InsertionText == "xmlns:")
					{
						return true;
					}

					// Re-parse after commit using current caret (pre-commit line end is stale).
					var caretAfter = _textView.Caret.Position.BufferPosition;
					var parser = ParseToCaret(caretAfter.Position);
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
						// Property insert is Classes="" (caret between quotes). The AttributeValue
						// branch otherwise never re-opens completion, so value lists never appear.
						if ((selected?.InsertionText?.EndsWith("\"\"", StringComparison.Ordinal) == true) &&
							!XamlEditCompleteness.IsFreeTextAttributeValue(parser))
						{
							TriggerCompletion();
						}
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
							skip = true;
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
			var parser = ParseToCaret(end);
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
			var parser = ParseToCaret(end);
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
			var parser = ParseToCaret(end);
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
			// Space inside an open tag switches from element names to attributes.
			if ((session != null) && !session.IsDismissed && (c == ' ') && IsCaretInsideOpenTag())
			{
				DismissSession(session);
				session = null;
			}

			if ((session == null) || session.IsDismissed)
			{
				if (!ShouldStartCompletionForContext(c))
				{
					return false;
				}

				if (TriggerCompletion() && (c != '<') && (c != '.') && (c != ' ') && (c != '[') && (c != '(') && (c != '|') && (c != '#') && (c != '/'))
				{
					_session?.Filter();
				}

				return true;
			}
		}
		else if (c == ',')
		{
			var session = _session;
			if (session is null || session.IsDismissed)
			{
				if (!IsCaretInsideOpenTag())
				{
					return false;
				}

				if (TriggerCompletion())
				{
					session?.Filter();
				}
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Letters and spaces in element content must not open the completion session.
	/// Free-text attribute values (Text="...") have no list either. Markup starters always may.
	/// </summary>
	private bool ShouldStartCompletionForContext(char c)
	{
		if ((c == '<') || (c == '{') || (c == '/') || (c == '.') ||
			(c == '[') || (c == '(') || (c == '|') || (c == '#') || (c == '$') ||
			(c == '^') || (c == ':') || (c == '\a'))
		{
			return true;
		}

		if ((c != '=') && (c != '"') && (c != '\'') && !IsCaretInsideOpenTag())
		{
			return false;
		}

		var caret = _textView.Caret.Position.BufferPosition.Position;
		return !XamlEditCompleteness.IsFreeTextAttributeValue(ParseToCaret(caret));
	}

	private bool IsCaretInsideOpenTag()
	{
		var snapshot = _textView.TextSnapshot;
		var pos = _textView.Caret.Position.BufferPosition.Position;
		pos = Math.Max(0, Math.Min(pos, snapshot.Length));
		var start = Math.Max(0, pos - 8192);
		var length = pos - start;
		if (length <= 0)
		{
			return false;
		}

		return XamlEditCompleteness.IsInsideOpenTag(snapshot.GetText(start, length));
	}

	private XmlParser ParseToCaret(int caret)
	{
		var snapshot = _textView.TextSnapshot;
		caret = Math.Max(0, Math.Min(caret, snapshot.Length));
		if (caret == 0)
		{
			return XmlParser.Parse(string.Empty);
		}

		return XmlParser.Parse(snapshot.GetText(0, caret).AsMemory(), 0, caret);
	}

	private bool HandleSessionUpdate()
	{
		var session = _session;
		if (session == null)
		{
			return false;
		}

		try
		{
			if (session.IsDismissed)
			{
				return false;
			}

			// Prefer Filter over Recalculate — Recalculate has hit
			// ShimCompletionController.RecalculateSession NREs in VS.
			session.Filter();

			// Filter with no remaining matches disposes the session.
			if (session.IsDismissed)
			{
				return true;
			}

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
		catch (ObjectDisposedException)
		{
			if (ReferenceEquals(_session, session))
			{
				_session = null;
			}

			return false;
		}
	}

	private static Microsoft.VisualStudio.Language.Intellisense.CompletionSet GetAvaloniaCompletionSet(
		ICompletionSession session)
	{
		try
		{
			if ((session == null) || session.IsDismissed || (session.CompletionSets == null))
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
		}
		catch (ObjectDisposedException)
		{
			return null;
		}
		catch (InvalidOperationException)
		{
			return null;
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

	private void OnGotAggregateFocus(object sender, EventArgs e)
	{
		PromoteCommandFilter();
	}

	private void OnTextViewClosed(object sender, EventArgs e)
	{
		_textView.GotAggregateFocus -= OnGotAggregateFocus;
		_textView.Closed -= OnTextViewClosed;
		if (_languageHeld)
		{
			_languageHeld = false;
			XmlEditorSmartIndent.ReleaseLanguageOff();
		}
	}

	/// <summary>
	/// Last AddCommandFilter wins the head of the chain. XML's ViewFilter is often
	/// added after us; if it stays first, Enter commit still runs HandleSmartIndent
	/// after ICustomCommit. Re-head so we swallow Enter/Tab and XML never sees them.
	/// </summary>
	private void PromoteCommandFilter()
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		if (_textViewAdapter == null)
		{
			return;
		}

		try
		{
			_textViewAdapter.RemoveCommandFilter(this);
		}
		catch
		{
			// Not in the chain yet.
		}

		_textViewAdapter.AddCommandFilter(this, out _nextCommandHandler);
	}

	private void SchedulePromoteCommandFilter()
	{
		PromoteCommandFilterAsync().FireAndForget();
	}

	private async Task PromoteCommandFilterAsync()
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
		await Task.Yield();
		if (_textView.IsClosed)
		{
			return;
		}

		PromoteCommandFilter();
		// After the view-creation message, not inside a keystroke. SetUserPreferences
		// re-enters the shell when it runs under the command filter.
		_languageHeld = true;
		XmlEditorSmartIndent.HoldLanguageOff(_serviceProvider, _textViewAdapter);
	}

	/// <summary>
	/// Ctrl+Space / Complete Word / List Members. The designer pane must handle these too:
	/// VS routes them to the doc view, and the XML language service often never delivers
	/// them to this text-view command filter.
	/// </summary>
	internal static bool IsInvokeCompletionCommand(Guid group, uint commandId)
	{
		if (group != VSConstants.VSStd2K)
		{
			return false;
		}

		var id = (VSConstants.VSStd2KCmdID) commandId;
		return id is VSConstants.VSStd2KCmdID.COMPLETEWORD
			or VSConstants.VSStd2KCmdID.SHOWMEMBERLIST
			or VSConstants.VSStd2KCmdID.AUTOCOMPLETE;
	}

	internal static bool TryInvokeFromTextView(ITextView textView)
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		if (textView == null)
		{
			LogCompletionStartFailed("Ctrl+Space: hosted text view is null");
			return false;
		}

		if (!textView.Properties.TryGetProperty(typeof(XamlCompletionCommandHandler), out XamlCompletionCommandHandler handler) ||
			(handler == null))
		{
			LogCompletionStartFailed("Ctrl+Space: no XamlCompletionCommandHandler on the text view");
			return false;
		}

		return handler.TryInvokeCompletion();
	}

	internal bool TryInvokeCompletion()
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		// Ctrl+Space must rebuild for the current caret (e.g. properties after "<Border ").
		// Reusing a leftover element session both shows the wrong list and Start() throws.
		DismissBrokerSessions();
		_deferringHostCompletion = false;
		if (!TriggerCompletion())
		{
			return false;
		}

		if (_deferringHostCompletion)
		{
			return true;
		}

		try
		{
			// Empty ApplicableTo (e.g. Classes="|") — Filter can dismiss a valid set.
			var set = GetAvaloniaCompletionSet(_session);
			var filterText = set?.ApplicableTo?.GetText(set.ApplicableTo.TextBuffer.CurrentSnapshot);
			if (!string.IsNullOrEmpty(filterText))
			{
				_session.Filter();
			}
		}
		catch (InvalidOperationException ex)
		{
			_session = null;
			LogCompletionStartFailed("Filter after start failed", ex);
			return false;
		}

		if (_session is { IsDismissed: false })
		{
			return true;
		}

		LogCompletionStartFailed("Session dismissed immediately after start");
		return false;
	}

	private bool TriggerCompletion()
	{
		try
		{
			return TriggerCompletionCore();
		}
		catch (Exception ex)
		{
			LogCompletionStartFailed("Unexpected exception while starting the completion session", ex);
			return false;
		}
	}

	private bool TriggerCompletionCore()
	{
		// The caret must be in a non-projection location.
		var caretPoint = _textView.Caret.Position.Point.GetPoint(
			x => !x.ContentType.IsOfType("projection"),
			PositionAffinity.Predecessor);

		if (!caretPoint.HasValue)
		{
			LogCompletionStartFailed("Caret is in a projection buffer; no completion point");
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
				try
				{
					if (sessions[i].IsDismissed)
					{
						continue;
					}

					IList<Microsoft.VisualStudio.Language.Intellisense.CompletionSet> sets;
					try
					{
						sets = sessions[i].CompletionSets;
					}
					catch (InvalidOperationException)
					{
						// Session created but not started — CompletionSets is illegal until Start.
						continue;
					}

					if ((sets == null) || (sets.Count == 0))
					{
						sessions[i].Dismiss();
						continue;
					}

					for (var j = sets.Count - 1; j >= 0; j--)
					{
						if (sets[j].Moniker.Equals("Avalonia"))
						{
							existingSession = sessions[i];
							break;
						}
					}
				}
				catch (ObjectDisposedException)
				{
					continue;
				}

				if (existingSession != null)
				{
					break;
				}
			}
		}

		if (existingSession != null)
		{
			if (!ReferenceEquals(_session, existingSession))
			{
				existingSession.Dismissed += SessionDismissed;
				_session = existingSession;
			}

			// Already started — calling Start() throws InvalidOperationException.
			if (existingSession.IsDismissed)
			{
				LogCompletionStartFailed("Reused Avalonia session was already dismissed");
				return false;
			}

			return true;
		}

		if (caretPoint.HasValue && TryQueueHostCompletion(caretPoint.Value))
		{
			return true;
		}

		var session = _completionBroker.CreateCompletionSession(
			_textView,
			caretPoint?.Snapshot.CreateTrackingPoint(caretPoint.Value.Position, PointTrackingMode.Positive),
			true);

		session.Dismissed += SessionDismissed;
		_session = session;
		try
		{
			session.Start();
		}
		catch (InvalidOperationException ex)
		{
			// Start() with no sets, already-started session, or disposed session
			// (ObjectDisposedException derives from InvalidOperationException).
			_session = null;
			try
			{
				session.Dismissed -= SessionDismissed;
				if (!session.IsDismissed)
				{
					session.Dismiss();
				}
			}
			catch
			{
				// ignore
			}

			LogCompletionStartFailed(
				"ICompletionSession.Start failed (usually no completion set — missing XAML metadata or empty engine result)",
				ex);
			return false;
		}

		if (session.IsDismissed)
		{
			LogCompletionStartFailed("Completion session dismissed during Start");
			return false;
		}

		return true;
	}

	private bool TryQueueHostCompletion(SnapshotPoint caretPoint)
	{
		if (!_textView.TextBuffer.Properties.TryGetProperty(typeof(XamlBufferMetadata), out XamlBufferMetadata metadata))
		{
			return false;
		}

		// Assembly paths mean the editor host owns the metadata graph. CompletionMetadata
		// stays null; the host call below fills the completion set before the session starts.
		var paths = metadata.AssemblyPaths;
		if ((paths == null) || (paths.Count == 0))
		{
			return false;
		}

		_textView.TextBuffer.Properties.TryGetProperty("AssemblyName", out string assemblyName);
		var pathCopy = new List<string>();
		for (var i = 0; i < paths.Count; i++)
		{
			pathCopy.Add(paths[i]);
		}

		List<string> extra = null;
		var names = _styleClassNameIndex?.GetNames();
		if (names != null)
		{
			extra = new List<string>();
			foreach (var name in names)
			{
				extra.Add(name);
			}
		}

		var snapshot = caretPoint.Snapshot;
		var version = snapshot.Version.VersionNumber;
		var tracking = snapshot.CreateTrackingPoint(caretPoint.Position, PointTrackingMode.Positive);
		_hostCompletionGeneration++;
		var generation = _hostCompletionGeneration;
		_deferringHostCompletion = true;
		StartHostCompletionAsync(
			generation,
			tracking,
			version,
			snapshot.GetText(),
			caretPoint.Position,
			assemblyName,
			extra,
			pathCopy).FireAndForget();
		return true;
	}

	private async Task StartHostCompletionAsync(
		int generation,
		ITrackingPoint tracking,
		int version,
		string text,
		int caret,
		string assemblyName,
		IList<string> extraClasses,
		IList<string> assemblyPaths)
	{
		GetCompletionsResponseMessage response;
		try
		{
			response = await EditorHostSession.GetCompletionsAsync(
				text,
				caret,
				assemblyName,
				extraClasses,
				assemblyPaths).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			Log.Logger.Error(ex, "XAML autocomplete failed on the editor host");
			return;
		}

		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
		if ((generation != _hostCompletionGeneration) || _textView.IsClosed)
		{
			return;
		}

		if (_textView.TextSnapshot.Version.VersionNumber != version)
		{
			return;
		}

		if (!string.IsNullOrEmpty(response.Error))
		{
			Log.Logger.Error("XAML autocomplete failed on the editor host: {Error}", response.Error);
			return;
		}

		var prepared = XamlCompletionSource.ToCompletionSet(response);
		if (prepared == null)
		{
			LogCompletionStartFailed("editor host returned no completions");
			return;
		}

		_textView.TextBuffer.Properties[XamlCompletionSource.PreparedCompletionsProperty] = prepared;
		var session = _completionBroker.CreateCompletionSession(_textView, tracking, true);
		session.Dismissed += SessionDismissed;
		_session = session;
		try
		{
			session.Start();
		}
		catch (InvalidOperationException ex)
		{
			_session = null;
			try
			{
				session.Dismissed -= SessionDismissed;
				if (!session.IsDismissed)
				{
					session.Dismiss();
				}
			}
			catch
			{
				// ignore
			}

			LogCompletionStartFailed("ICompletionSession.Start failed after the editor host replied", ex);
			return;
		}

		if (session.IsDismissed)
		{
			LogCompletionStartFailed("Completion session dismissed during Start");
			return;
		}

		try
		{
			var set = GetAvaloniaCompletionSet(_session);
			var filterText = set?.ApplicableTo?.GetText(set.ApplicableTo.TextBuffer.CurrentSnapshot);
			if (!string.IsNullOrEmpty(filterText))
			{
				_session.Filter();
			}
		}
		catch (InvalidOperationException ex)
		{
			_session = null;
			LogCompletionStartFailed("Filter after start failed", ex);
		}
	}

	private bool BeginEnterIndent()
	{
		try
		{
			var selection = _textView.Selection;
			var caret = _textView.Caret.Position.BufferPosition;
			var snapshot = caret.Snapshot;
			var replaceStart = caret.Position;
			var replaceLength = 0;
			if ((selection != null) && !selection.IsEmpty && (selection.SelectedSpans.Count > 0))
			{
				var span = selection.SelectedSpans[0];
				replaceStart = span.Start.Position;
				replaceLength = span.Length;
				caret = span.Start;
				snapshot = span.Start.Snapshot;
			}

			var line = snapshot.GetLineFromPosition(caret.Position);
			var caretIndex = caret.Position - line.Start.Position;
			var previousLine = string.Empty;
			if (line.LineNumber > 0)
			{
				previousLine = snapshot.GetLineFromLineNumber(line.LineNumber - 1).GetText();
			}

			var indentSize = 4;
			if (_textView.Options != null)
			{
				indentSize = _textView.Options.GetIndentSize();
			}

			var newLine = (_textView.Options != null)
				? _textView.Options.GetNewLineCharacter()
				: "\r\n";

			var version = snapshot.Version.VersionNumber;
			var tracking = snapshot.CreateTrackingPoint(replaceStart, PointTrackingMode.Negative);
			ApplyEnterIndentAsync(
				tracking,
				replaceLength,
				version,
				line.GetText(),
				caretIndex,
				previousLine,
				indentSize,
				newLine).FireAndForget();
			return true;
		}
		catch (Exception ex)
		{
			Log.Logger.Error(ex, "XAML enter indent failed");
			return false;
		}
	}

	private async Task ApplyEnterIndentAsync(
		ITrackingPoint tracking,
		int replaceLength,
		int version,
		string line,
		int caretIndex,
		string previousLine,
		int indentSize,
		string newLine)
	{
		string insertion;
		try
		{
			var response = await EditorHostSession.BuildEnterIndentAsync(
				line,
				caretIndex,
				previousLine,
				indentSize,
				newLine).ConfigureAwait(false);
			if (!string.IsNullOrEmpty(response.Error))
			{
				Log.Logger.Error("XAML enter indent failed: {Error}", response.Error);
				return;
			}

			insertion = response.Insertion;
		}
		catch (Exception ex)
		{
			Log.Logger.Error(ex, "XAML enter indent failed");
			return;
		}

		if (string.IsNullOrEmpty(insertion))
		{
			return;
		}

		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
		if (_textView.IsClosed || (tracking.TextBuffer != _textView.TextBuffer))
		{
			return;
		}

		var snapshot = _textView.TextSnapshot;
		if (snapshot.Version.VersionNumber != version)
		{
			return;
		}

		var replaceStart = tracking.GetPosition(snapshot);
		ITextUndoTransaction transaction = null;
		try
		{
			if (_textUndoHistoryRegistry != null)
			{
				var history = _textUndoHistoryRegistry.RegisterHistory(snapshot.TextBuffer);
				transaction = history.CreateTransaction("Insert New Line");
			}

			using (var edit = snapshot.TextBuffer.CreateEdit())
			{
				edit.Replace(replaceStart, replaceLength, insertion);
				edit.Apply();
			}

			transaction?.Complete();
		}
		catch (Exception ex)
		{
			Log.Logger.Error(ex, "XAML enter indent failed");
		}
		finally
		{
			transaction?.Dispose();
		}
	}

	private static void LogCompletionStartFailed(string reason, Exception ex = null)
	{
		if (ex != null)
		{
			Log.Logger.Error(ex, "XAML autocomplete failed to start: {Reason}", reason);
		}
		else
		{
			Log.Logger.Error("XAML autocomplete failed to start: {Reason}", reason);
		}
	}

	private void DismissBrokerSessions()
	{
		DismissSession(_session);
		IList<ICompletionSession> sessions;
		try
		{
			sessions = _completionBroker.GetSessions(_textView);
		}
		catch
		{
			return;
		}

		for (var i = sessions.Count - 1; i >= 0; i--)
		{
			DismissSession(sessions[i]);
		}
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
				// Ctrl+Space is handled in Exec (must not be treated as a space, which commits).
				case VSConstants.VSStd2KCmdID.COMPLETEWORD:
					c = '\a';
					break;
			}
		}

		return c != '\0';
	}

	#endregion
}
