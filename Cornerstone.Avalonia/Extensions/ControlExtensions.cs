#region References

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Cornerstone.Avalonia.TextEditor.Utils;

#endregion

namespace Cornerstone.Avalonia.Extensions;

public static class ControlExtensions
{
	#region Methods

	public static Size GetTextSize(this Control control, string text = null)
	{
		var formattedText = TextFormatterFactory.CreateFormattedText(
			control,
			text ?? new string('9', 1),
			control.CreateTypeface(),
			control.GetValue(TemplatedControl.FontSizeProperty),
			control.GetValue(TemplatedControl.ForegroundProperty)
		);
		return new Size(formattedText.Width, formattedText.Height);
	}

	#endregion
}