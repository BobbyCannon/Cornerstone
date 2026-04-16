#region References

using System;
using System.Runtime.CompilerServices;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Avalonia.Text.Models;

[SourceReflection]
public partial class Selection : Notifiable
{
	#region Fields

	private readonly Caret _caret;

	#endregion

	#region Constructors

	public Selection(Caret caret)
	{
		_caret = caret;
	}

	#endregion

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

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public bool Contains(int offset)
	{
		var selLeft = Math.Min(StartOffset, EndOffset);
		var selRight = Math.Max(StartOffset, EndOffset);
		var response = (offset >= selLeft) && (offset <= selRight);
		return response;
	}

	/// <summary>
	/// Implicitly converts the Selection to Avalonia TextSelection.
	/// </summary>
	public static implicit operator TextSelection(Selection selection)
	{
		return new TextSelection(selection.StartOffset, selection.EndOffset);
	}

	public void ProcessKeyDown(KeyEventArgs args)
	{
		var shouldBeSelecting = ((args.KeyModifiers & KeyModifiers.Shift) != 0)
			&& args.Key is Key.Left or Key.Right or Key.Up or Key.Down or
				Key.Home or Key.End or Key.PageUp or Key.PageDown;

		if (IsSelectingUsingKeyboard && !shouldBeSelecting)
		{
			IsSelectingUsingKeyboard = false;
		}
		if (!IsSelectingUsingKeyboard && shouldBeSelecting)
		{
			Reset(_caret.Offset);
			IsSelectingUsingKeyboard = true;
		}

		RefreshSelection();
	}

	public void ProcessKeyUp(KeyEventArgs args)
	{
		if (args.Key is Key.LeftShift or Key.RightShift)
		{
			StopKeyboardSelection();
		}

		RefreshSelection();
	}

	public void Reset(int offset = 0)
	{
		Update(offset, offset);
		IsSelectingUsingKeyboard = false;
		IsSelectingUsingMouse = false;
	}

	public void StartKeyboardSelection(int offset)
	{
		Reset(offset);
		IsSelectingUsingKeyboard = true;
		OnUpdated();
	}

	public void StartMouseSelection()
	{
		IsSelectingUsingMouse = true;
		OnUpdated();
	}

	public void StopKeyboardSelection()
	{
		IsSelectingUsingKeyboard = false;
		OnUpdated();
	}

	public void StopMouseSelection()
	{
		IsSelectingUsingMouse = false;
		OnUpdated();
	}

	public void StopSelection()
	{
		IsSelectingUsingKeyboard = false;
		IsSelectingUsingMouse = false;
		OnUpdated();
	}

	public void Update(int start, int end)
	{
		StartOffset = start;
		EndOffset = end;
		OnUpdated();
	}

	public void Update(int end)
	{
		EndOffset = end;
		OnUpdated();
	}

	public void Update(TextSelection selection)
	{
		StartOffset = selection.Start;
		EndOffset = selection.End;
		OnUpdated();
	}

	protected virtual void OnUpdated()
	{
		Updated?.Invoke(this, EventArgs.Empty);
	}

	private void RefreshSelection()
	{
		// We will clear if the caret goes outside the selection and we are not selecting
		var shouldClearSelection = (Length > 0) && !Contains(_caret.Offset);
		if (!IsSelecting && shouldClearSelection)
		{
			Reset(_caret.Offset);
		}
	}

	#endregion

	#region Events

	public event EventHandler Updated;

	#endregion
}