#region References

using System;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Document;

/// <summary>
/// Contains weak event managers for the TextDocument events.
/// </summary>
public static class TextDocumentWeakEventManager
{
	#region Classes

	/// <summary>
	/// Weak event manager for the <see cref="TextEditorDocument.Changed" /> event.
	/// </summary>
	[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
	public sealed class Changed : WeakEventManagerBase<Changed, TextEditorDocument, EventHandler<DocumentChangeEventArgs>, DocumentChangeEventArgs>
	{
		#region Methods

		protected override void StartListening(TextEditorDocument source)
		{
			source.Changed += DeliverEvent;
		}

		protected override void StopListening(TextEditorDocument source)
		{
			source.Changed -= DeliverEvent;
		}

		#endregion
	}

	/// <summary>
	/// Weak event manager for the <see cref="TextEditorDocument.Changing" /> event.
	/// </summary>
	[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
	public sealed class Changing : WeakEventManagerBase<Changing, TextEditorDocument, EventHandler<DocumentChangeEventArgs>, DocumentChangeEventArgs>
	{
		#region Methods

		protected override void StartListening(TextEditorDocument source)
		{
			source.Changing += DeliverEvent;
		}

		protected override void StopListening(TextEditorDocument source)
		{
			source.Changing -= DeliverEvent;
		}

		#endregion
	}

	/// <summary>
	/// Weak event manager for the <see cref="TextEditorDocument.LineCountChanged" /> event.
	/// </summary>
	[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
	public sealed class LineCountChanged : WeakEventManagerBase<LineCountChanged, TextEditorDocument, EventHandler, EventArgs>
	{
		#region Methods

		protected override void StartListening(TextEditorDocument source)
		{
			source.LineCountChanged += DeliverEvent;
		}

		protected override void StopListening(TextEditorDocument source)
		{
			source.LineCountChanged -= DeliverEvent;
		}

		#endregion
	}

	/// <summary>
	/// Weak event manager for the <see cref="TextEditorDocument.TextChanged" /> event.
	/// </summary>
	[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
	public sealed class TextChanged : WeakEventManagerBase<TextChanged, TextEditorDocument, EventHandler, EventArgs>
	{
		#region Methods

		protected override void StartListening(TextEditorDocument source)
		{
			source.TextChanged += DeliverEvent;
		}

		protected override void StopListening(TextEditorDocument source)
		{
			source.TextChanged -= DeliverEvent;
		}

		#endregion
	}

	/// <summary>
	/// Weak event manager for the <see cref="TextEditorDocument.TextLengthChanged" /> event.
	/// </summary>
	[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
	public sealed class TextLengthChanged : WeakEventManagerBase<TextLengthChanged, TextEditorDocument, EventHandler, EventArgs>
	{
		#region Methods

		protected override void StartListening(TextEditorDocument source)
		{
			source.TextLengthChanged += DeliverEvent;
		}

		protected override void StopListening(TextEditorDocument source)
		{
			source.TextLengthChanged -= DeliverEvent;
		}

		#endregion
	}

	/// <summary>
	/// Weak event manager for the <see cref="TextEditorDocument.UpdateFinished" /> event.
	/// </summary>
	[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
	public sealed class UpdateFinished : WeakEventManagerBase<UpdateFinished, TextEditorDocument, EventHandler, EventArgs>
	{
		#region Methods

		protected override void StartListening(TextEditorDocument source)
		{
			source.UpdateFinished += DeliverEvent;
		}

		protected override void StopListening(TextEditorDocument source)
		{
			source.UpdateFinished -= DeliverEvent;
		}

		#endregion
	}

	/// <summary>
	/// Weak event manager for the <see cref="TextEditorDocument.UpdateStarted" /> event.
	/// </summary>
	[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
	public sealed class UpdateStarted : WeakEventManagerBase<UpdateStarted, TextEditorDocument, EventHandler, EventArgs>
	{
		#region Methods

		protected override void StartListening(TextEditorDocument source)
		{
			source.UpdateStarted += DeliverEvent;
		}

		protected override void StopListening(TextEditorDocument source)
		{
			source.UpdateStarted -= DeliverEvent;
		}

		#endregion
	}

	#endregion
}