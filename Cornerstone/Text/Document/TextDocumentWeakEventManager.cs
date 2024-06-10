#region References

using System;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Text.Document;

/// <summary>
/// Contains weak event managers for the TextDocument events.
/// </summary>
public static class TextDocumentWeakEventManager
{
	#region Classes

	/// <summary>
	/// Weak event manager for the <see cref="TextDocument.Changed" /> event.
	/// </summary>
	[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
	public sealed class Changed : WeakEventManagerBase<Changed, TextDocument, EventHandler<DocumentChangeEventArgs>, DocumentChangeEventArgs>
	{
		#region Methods

		protected override void StartListening(TextDocument source)
		{
			source.Changed += DeliverEvent;
		}

		protected override void StopListening(TextDocument source)
		{
			source.Changed -= DeliverEvent;
		}

		#endregion
	}

	/// <summary>
	/// Weak event manager for the <see cref="TextDocument.Changing" /> event.
	/// </summary>
	[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
	public sealed class Changing : WeakEventManagerBase<Changing, TextDocument, EventHandler<DocumentChangeEventArgs>, DocumentChangeEventArgs>
	{
		#region Methods

		protected override void StartListening(TextDocument source)
		{
			source.Changing += DeliverEvent;
		}

		protected override void StopListening(TextDocument source)
		{
			source.Changing -= DeliverEvent;
		}

		#endregion
	}

	/// <summary>
	/// Weak event manager for the <see cref="TextDocument.LineCountChanged" /> event.
	/// </summary>
	[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
	public sealed class LineCountChanged : WeakEventManagerBase<LineCountChanged, TextDocument, EventHandler, EventArgs>
	{
		#region Methods

		protected override void StartListening(TextDocument source)
		{
			source.LineCountChanged += DeliverEvent;
		}

		protected override void StopListening(TextDocument source)
		{
			source.LineCountChanged -= DeliverEvent;
		}

		#endregion
	}

	/// <summary>
	/// Weak event manager for the <see cref="TextDocument.TextChanged" /> event.
	/// </summary>
	[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
	public sealed class TextChanged : WeakEventManagerBase<TextChanged, TextDocument, EventHandler, EventArgs>
	{
		#region Methods

		protected override void StartListening(TextDocument source)
		{
			source.TextChanged += DeliverEvent;
		}

		protected override void StopListening(TextDocument source)
		{
			source.TextChanged -= DeliverEvent;
		}

		#endregion
	}

	/// <summary>
	/// Weak event manager for the <see cref="TextDocument.TextLengthChanged" /> event.
	/// </summary>
	[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
	public sealed class TextLengthChanged : WeakEventManagerBase<TextLengthChanged, TextDocument, EventHandler, EventArgs>
	{
		#region Methods

		protected override void StartListening(TextDocument source)
		{
			source.TextLengthChanged += DeliverEvent;
		}

		protected override void StopListening(TextDocument source)
		{
			source.TextLengthChanged -= DeliverEvent;
		}

		#endregion
	}

	/// <summary>
	/// Weak event manager for the <see cref="TextDocument.UpdateFinished" /> event.
	/// </summary>
	[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
	public sealed class UpdateFinished : WeakEventManagerBase<UpdateFinished, TextDocument, EventHandler, EventArgs>
	{
		#region Methods

		protected override void StartListening(TextDocument source)
		{
			source.UpdateFinished += DeliverEvent;
		}

		protected override void StopListening(TextDocument source)
		{
			source.UpdateFinished -= DeliverEvent;
		}

		#endregion
	}

	/// <summary>
	/// Weak event manager for the <see cref="TextDocument.UpdateStarted" /> event.
	/// </summary>
	[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
	public sealed class UpdateStarted : WeakEventManagerBase<UpdateStarted, TextDocument, EventHandler, EventArgs>
	{
		#region Methods

		protected override void StartListening(TextDocument source)
		{
			source.UpdateStarted += DeliverEvent;
		}

		protected override void StopListening(TextDocument source)
		{
			source.UpdateStarted -= DeliverEvent;
		}

		#endregion
	}

	#endregion
}