namespace Cornerstone.Avalonia.Text.Models;

public enum CaretMoveDirection
{
	Unknown = 0,
	Backspace,
	CharLeft,
	CharRight,
	DocumentEnd,
	DocumentStart,
	LineDown,
	LineEnd,
	LineUp,
	LineStart,
	LineSmartStart,
	PageDown,
	PageUp,
	WordLeft,
	WordRight
}