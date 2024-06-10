#region References

using System;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Avalonia.AvaloniaEdit.Utils;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Editing;

/// <summary>
/// Contains classes for handling weak events on the Caret class.
/// </summary>
public static class CaretWeakEventManager
{
	#region Classes

	/// <summary>
	/// Handles the Caret.PositionChanged event.
	/// </summary>
	[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
	public sealed class PositionChanged : WeakEventManagerBase<PositionChanged, Caret, EventHandler, EventArgs>
	{
		#region Methods

		protected override void StartListening(Caret source)
		{
			source.PositionChanged += DeliverEvent;
		}

		protected override void StopListening(Caret source)
		{
			source.PositionChanged -= DeliverEvent;
		}

		#endregion
	}

	#endregion
}