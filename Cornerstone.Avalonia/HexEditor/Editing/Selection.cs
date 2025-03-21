#region References

using System;
using Cornerstone.Avalonia.HexEditor.Document;
using Cornerstone.Avalonia.HexEditor.Rendering;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Avalonia.HexEditor.Editing;

/// <summary>
/// Represents a selection within a hex editor.
/// </summary>
public class Selection : Bindable
{
	#region Fields

	private BitRange _range;

	#endregion

	#region Constructors

	internal Selection(HexView hexView) : base(hexView.GetDispatcher())
	{
		HexView = hexView;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the hex view the selection is rendered on.
	/// </summary>
	public HexView HexView { get; }

	/// <summary>
	/// Gets or sets the range the selection spans.
	/// </summary>
	public BitRange Range
	{
		get => _range;
		set
		{
			value = HexView.Document is { } document
				? value.Clamp(document.ValidRanges.EnclosingRange)
				: BitRange.Empty;

			if (_range != value)
			{
				_range = value;
				OnRangeChanged();
			}
		}
	}

	#endregion

	#region Methods

	/// <summary>
	/// Selects the entire document.
	/// </summary>
	public void SelectAll()
	{
		Range = HexView.Document is not null
			? new BitRange(0, HexView.Document.Length)
			: default;
	}

	private void OnRangeChanged()
	{
		RangeChanged?.Invoke(this, EventArgs.Empty);
	}

	#endregion

	#region Events

	/// <summary>
	/// Fires when the selection range has changed.
	/// </summary>
	public event EventHandler RangeChanged;

	#endregion
}