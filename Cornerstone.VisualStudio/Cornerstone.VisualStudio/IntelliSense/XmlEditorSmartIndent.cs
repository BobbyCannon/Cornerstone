#region References

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using IServiceProvider = System.IServiceProvider;

#endregion

namespace Cornerstone.VisualStudio.IntelliSense;

/// <summary>
/// The XML language service's ViewFilter.HandlePostExec runs after our ICustomCommit
/// on Enter when XML is still ahead in the command chain. It does not treat a MEF
/// ICompletionSession as a completor, so it calls HandleSmartIndent.
/// The text view indent style is cleared for that one command.
/// The language preference is held off while any designer view is open. Changing it
/// inside the commit command calls IVsTextManager.SetUserPreferences on the UI thread,
/// and that re-enters the shell until another window message arrives.
/// </summary>
internal static class XmlEditorSmartIndent
{
	#region Fields

	private static readonly object _syncLockForIndent;
	private static int _depth;
	private static int _holders;
	private static bool _languageChecked;
	private static bool _xmlApplied;
	private static Guid _langGuid;
	private static vsIndentStyle _previousXmlStyle;
	private static IVsTextManager _textManager;
	private static IndentingStyle _previousViewStyle;
	private static IEditorOptions _viewOptions;

	#endregion

	#region Constructors

	static XmlEditorSmartIndent()
	{
		_syncLockForIndent = new object();
		_depth = 0;
		_holders = 0;
		_languageChecked = false;
		_xmlApplied = false;
		_langGuid = Guid.Empty;
		_previousXmlStyle = vsIndentStyle.vsIndentStyleSmart;
		_textManager = null;
		_previousViewStyle = IndentingStyle.Smart;
		_viewOptions = null;
	}

	#endregion

	#region Methods

	/// <summary>
	/// Turn off the text view's smart indent for the rest of this command.
	/// Restore is queued so it does not run before ViewFilter.HandlePostExec.
	/// </summary>
	public static void SuppressUntilAfterCurrentCommand(ITextView textView)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		lock (_syncLockForIndent)
		{
			if (_depth == 0)
			{
				SuppressView(textView);
			}

			_depth++;
		}

		ScheduleRestore();
	}

	/// <summary>
	/// Hold the XML language indent off for one open designer view. Call on the UI
	/// thread after the view exists, not from inside a command filter.
	/// </summary>
	public static void HoldLanguageOff(IServiceProvider services, IVsTextView textViewAdapter)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		var apply = false;
		lock (_syncLockForIndent)
		{
			_holders++;
			apply = !_languageChecked;
		}

		if (apply)
		{
			SuppressXmlLanguage(services, textViewAdapter);
		}
	}

	/// <summary>
	/// Drops one designer view. The last view restores the language indent preference.
	/// </summary>
	public static void ReleaseLanguageOff()
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		var restore = false;
		lock (_syncLockForIndent)
		{
			if (_holders > 0)
			{
				_holders--;
			}

			restore = (_holders == 0) && _xmlApplied;
			if (_holders == 0)
			{
				_languageChecked = false;
			}
		}

		if (restore)
		{
			RestoreLanguagePreferences();
		}
	}

	private static void SuppressView(ITextView textView)
	{
		if (textView?.Options == null)
		{
			return;
		}

		_viewOptions = textView.Options;
		_previousViewStyle = _viewOptions.GetOptionValue(DefaultOptions.IndentStyleId);
		_viewOptions.SetOptionValue(DefaultOptions.IndentStyleId, IndentingStyle.None);
	}

	private static void SuppressXmlLanguage(IServiceProvider services, IVsTextView textViewAdapter)
	{
		if ((services == null) || (textViewAdapter == null))
		{
			return;
		}

		if (textViewAdapter.GetBuffer(out var lines) != 0 || (lines == null))
		{
			return;
		}

		var buffer = (IVsTextBuffer) lines;
		if (buffer.GetLanguageServiceID(out var langGuid) != 0 || (langGuid == Guid.Empty))
		{
			return;
		}

		var textManager = services.GetService(typeof(SVsTextManager)) as IVsTextManager;
		if (textManager == null)
		{
			return;
		}

		var prefs = new LANGPREFERENCES[1];
		prefs[0].guidLang = langGuid;
		if (textManager.GetUserPreferences(null, null, prefs, null) != 0)
		{
			return;
		}

		_languageChecked = true;
		if (prefs[0].IndentStyle != vsIndentStyle.vsIndentStyleSmart)
		{
			return;
		}

		_textManager = textManager;
		_langGuid = langGuid;
		_previousXmlStyle = prefs[0].IndentStyle;
		prefs[0].IndentStyle = vsIndentStyle.vsIndentStyleNone;
		if (textManager.SetUserPreferences(null, null, prefs, null) == 0)
		{
			_xmlApplied = true;
		}
	}

	private static void ScheduleRestore()
	{
		RestoreAfterCommandAsync().FireAndForget();
	}

	private static async Task RestoreAfterCommandAsync()
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
		await Task.Yield();
		RestoreOne();
	}

	private static void RestoreOne()
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		lock (_syncLockForIndent)
		{
			if (_depth > 0)
			{
				_depth--;
			}

			if (_depth > 0)
			{
				return;
			}

			if (_viewOptions != null)
			{
				_viewOptions.SetOptionValue(DefaultOptions.IndentStyleId, _previousViewStyle);
				_viewOptions = null;
			}
		}
	}

	private static void RestoreLanguagePreferences()
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		IVsTextManager textManager;
		Guid langGuid;
		vsIndentStyle previous;
		lock (_syncLockForIndent)
		{
			if (!_xmlApplied || (_textManager == null) || (_langGuid == Guid.Empty))
			{
				_xmlApplied = false;
				_textManager = null;
				_langGuid = Guid.Empty;
				return;
			}

			textManager = _textManager;
			langGuid = _langGuid;
			previous = _previousXmlStyle;
			_xmlApplied = false;
			_textManager = null;
			_langGuid = Guid.Empty;
		}

		var prefs = new LANGPREFERENCES[1];
		prefs[0].guidLang = langGuid;
		if (textManager.GetUserPreferences(null, null, prefs, null) == 0)
		{
			prefs[0].IndentStyle = previous;
			textManager.SetUserPreferences(null, null, prefs, null);
		}
	}

	#endregion
}
