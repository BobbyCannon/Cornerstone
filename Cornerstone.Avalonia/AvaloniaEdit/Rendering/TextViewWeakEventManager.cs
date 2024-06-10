#region References

using System;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Avalonia.AvaloniaEdit.Utils;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Rendering;

/// <summary>
/// Contains weak event managers for the TextView events.
/// </summary>
public static class TextViewWeakEventManager
{
	#region Classes

	/// <summary>
	/// Weak event manager for the <see cref="TextView.DocumentChanged" /> event.
	/// </summary>
	[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
	public sealed class DocumentChanged : WeakEventManagerBase<DocumentChanged, TextView, EventHandler, EventArgs>
	{
		#region Methods

		protected override void StartListening(TextView source)
		{
			source.DocumentChanged += DeliverEvent;
		}

		protected override void StopListening(TextView source)
		{
			source.DocumentChanged -= DeliverEvent;
		}

		#endregion
	}

	/// <summary>
	/// Weak event manager for the <see cref="TextView.ScrollOffsetChanged" /> event.
	/// </summary>
	[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
	public sealed class ScrollOffsetChanged : WeakEventManagerBase<ScrollOffsetChanged, TextView, EventHandler, EventArgs>
	{
		#region Methods

		protected override void StartListening(TextView source)
		{
			source.ScrollOffsetChanged += DeliverEvent;
		}

		protected override void StopListening(TextView source)
		{
			source.ScrollOffsetChanged -= DeliverEvent;
		}

		#endregion
	}

	/// <summary>
	/// Weak event manager for the <see cref="TextView.VisualLinesChanged" /> event.
	/// </summary>
	[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
	public sealed class VisualLinesChanged : WeakEventManagerBase<VisualLinesChanged, TextView, EventHandler, EventArgs>
	{
		#region Methods

		protected override void StartListening(TextView source)
		{
			source.VisualLinesChanged += DeliverEvent;
		}

		protected override void StopListening(TextView source)
		{
			source.VisualLinesChanged -= DeliverEvent;
		}

		#endregion
	}

	#endregion
}