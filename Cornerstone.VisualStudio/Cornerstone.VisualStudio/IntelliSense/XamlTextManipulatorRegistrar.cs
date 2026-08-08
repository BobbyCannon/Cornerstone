#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cornerstone.VisualStudio.Core.Manipulation;
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
				var sw = Stopwatch.StartNew();
				var text = _buffer.CurrentSnapshot.GetText();

				foreach (var change in e.Changes.ToList())
				{
					// Guard: NewPosition can be at EOF after delete.
					if (text.Length == 0)
					{
						continue;
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