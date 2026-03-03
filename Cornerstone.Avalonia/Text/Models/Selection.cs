#region References

using System;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Avalonia.Text.Models;

public partial class Selection : Notifiable<Selection>
{
	#region Properties

	/// <summary>
	/// The end offset (exclusive)  of the selection.
	/// </summary>
	[Notify]
	[AlsoNotify(nameof(Length))]
	[UpdateableAction(UpdateableAction.All)]
	public partial int EndOffset { get; set; }

	public bool IsSelecting => IsSelectingUsingKeyboard || IsSelectingUsingMouse;

	/// <summary>
	/// The flag to indicate selection is being made with keyboard.
	/// </summary>
	[Notify]
	[AlsoNotify(nameof(Length))]
	[UpdateableAction(UpdateableAction.All)]
	public partial bool IsSelectingUsingKeyboard { get; private set; }

	/// <summary>
	/// The flag to indicate selection is being made with mouse.
	/// </summary>
	[Notify]
	[AlsoNotify(nameof(Length))]
	[UpdateableAction(UpdateableAction.All)]
	public partial bool IsSelectingUsingMouse { get; private set; }

	/// <summary>
	/// The length of the selection.
	/// </summary>
	public int Length => Math.Abs(EndOffset - StartOffset);

	/// <summary>
	/// The start offset (inclusive) of the selection.
	/// </summary>
	[Notify]
	[AlsoNotify(nameof(Length))]
	[UpdateableAction(UpdateableAction.All)]
	public partial int StartOffset { get; set; }

	#endregion

	#region Methods

	public void EndKeyboardSelection()
	{
		IsSelectingUsingKeyboard = false;
	}

	public void EndMouseSelection()
	{
		IsSelectingUsingMouse = false;
	}

	public void EndSelection()
	{
		IsSelectingUsingKeyboard = false;
		IsSelectingUsingMouse = false;
	}

	public void Reset(int offset = 0)
	{
		StartOffset = offset;
		EndOffset = offset;
	}

	public void StartKeyboardSelection(int offset)
	{
		Reset(offset);
		IsSelectingUsingKeyboard = true;
	}

	public void StartMouseSelection()
	{
		IsSelectingUsingMouse = true;
	}

	#endregion
}