#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cornerstone.VisualStudio.Core.Manipulation;
using Cornerstone.VisualStudio.Core.Parsing;
using Cornerstone.VisualStudio.Models;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Serilog;

#endregion

namespace Cornerstone.VisualStudio.IntelliSense;

internal class XamlTextManipulatorRegistrar
{
	#region Fields

	private readonly ITextBuffer _buffer;
	private bool _isChangingText;
	private readonly IWpfTextView _textView;

	/// <summary>
	/// Characters before the caret to inspect when deciding if we are inside a tag.
	/// </summary>
	private const int OpenTagLookback = 8192;

	/// <summary>
	/// When &gt; 0, buffer changes from IntelliSense commit (etc.) skip auto tag manipulators.
	/// </summary>
	private static int _suppressDepth;

	#endregion

	#region Constructors

	public XamlTextManipulatorRegistrar(IWpfTextView textView)
	{
		_textView = textView;
		_buffer = textView.TextBuffer;

		_textView.Closed += TextView_Closed;
		_buffer.Changed += TextBuffer_Changed;
	}

	#endregion

	#region Methods

	/// <summary>
	/// Suppress start/end-tag sync while applying a completion replace.
	/// </summary>
	public static IDisposable Suppress()
	{
		_suppressDepth++;
		return new SuppressScope();
	}

	private sealed class SuppressScope : IDisposable
	{
		private bool _disposed;

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			if (_suppressDepth > 0)
			{
				_suppressDepth--;
			}
		}
	}

	private void ApplyManipulations(IList<TextManipulation> manipulations)
	{
		var edit = _buffer.CreateEdit();
		foreach (var manipulation in manipulations)
		{
			switch (manipulation.Type)
			{
				case ManipulationType.Insert:
					edit.Insert(manipulation.Start, manipulation.Text);
					break;
				case ManipulationType.Delete:
					if ((manipulation.Start >= 0) &&
						(manipulation.End > manipulation.Start) &&
						(manipulation.End <= _buffer.CurrentSnapshot.Length))
					{
						edit.Delete(Span.FromBounds(manipulation.Start, manipulation.End));
					}
					break;
			}
		}
		edit.Apply();
	}

	private void TextBuffer_Changed(object sender, TextContentChangedEventArgs e)
	{
		if (_isChangingText || (_suppressDepth > 0))
		{
			return;
		}

		try
		{
			if (_buffer.Properties.TryGetProperty<XamlBufferMetadata>(typeof(XamlBufferMetadata), out var metadata) &&
				(metadata.CompletionMetadata != null))
			{
				var snapshot = _buffer.CurrentSnapshot;
				if (snapshot.Length == 0)
				{
					return;
				}

				string text = null;
				var sw = Stopwatch.StartNew();

				foreach (var change in e.Changes.ToList())
				{
					if (!ShouldRunManipulator(snapshot, change))
					{
						continue;
					}

					// Full snapshot only when a change can affect tags (not every content keystroke).
					text ??= snapshot.GetText();
					if (text.Length == 0)
					{
						break;
					}

					var pos = Math.Min(Math.Max(0, change.NewPosition), text.Length - 1);
					var textManipulator = new TextManipulator(text, pos);
					var avaloniaChange = new TextChangeAdapter(change);
					var manipulations = textManipulator.ManipulateText(avaloniaChange);
					if (manipulations?.Count > 0)
					{
						_isChangingText = true;
						ApplyManipulations(manipulations);
						Log.Logger.Verbose("XAML manipulation took {Time}", sw.Elapsed);
					}
				}
				sw.Stop();
			}
		}
		catch (Exception ex)
		{
			// Never let manipulators take down the editor (ActivityLog IndexOutOfRange, etc.).
			Log.Logger.Debug(ex, "XAML text manipulator failed");
		}
		finally
		{
			_isChangingText = false;
		}
	}

	/// <summary>
	/// Tag sync is only needed inside an open tag, or when the edit itself contains markup.
	/// Typing element content must not allocate or parse the whole document.
	/// </summary>
	private static bool ShouldRunManipulator(ITextSnapshot snapshot, ITextChange change)
	{
		if (XamlEditCompleteness.ChangeLooksLikeMarkup(change.OldText, change.NewText))
		{
			return true;
		}

		var pos = Math.Min(Math.Max(0, change.NewPosition), snapshot.Length);
		var lookbackStart = Math.Max(0, pos - OpenTagLookback);
		var length = pos - lookbackStart;
		if (length <= 0)
		{
			return false;
		}

		var prefix = snapshot.GetText(lookbackStart, length);
		return XamlEditCompleteness.IsInsideOpenTag(prefix);
	}

	private void TextView_Closed(object sender, EventArgs e)
	{
		if (_textView != null)
		{
			_textView.Closed -= TextView_Closed;
			_buffer.Changed -= TextBuffer_Changed;
		}
	}

	#endregion
}