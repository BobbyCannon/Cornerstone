#region References

using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Avalonia.FileEditor;

/// <summary>
/// A control that allows for displaying and editing files.
/// </summary>
public class FileEditor : CornerstoneTemplatedControl
{
	#region Fields

	private readonly SpeedyList<VisualLine> _visualLines;

	#endregion

	#region Constructors

	public FileEditor()
	{
		_visualLines = new SpeedyList<VisualLine>();

		EnsureTextProperties();
	}

	static FileEditor()
	{
		FontFamilyProperty.Changed.AddClassHandler<FileEditor>(OnFontRelatedPropertyChanged);
		FontSizeProperty.Changed.AddClassHandler<FileEditor>(OnFontRelatedPropertyChanged);
		ForegroundProperty.Changed.AddClassHandler<FileEditor>(OnFontRelatedPropertyChanged);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the text run properties that are used for rendering the text in the hex view.
	/// </summary>
	public GenericTextRunProperties TextRunProperties { get; private set; }

	/// <summary>
	/// Gets the typeface that is used for rendering the text in the hex view.
	/// </summary>
	public Typeface Typeface { get; private set; }

	/// <summary>
	/// Gets the current lines that are visible.
	/// </summary>
	public ReadOnlySpeedyList<VisualLine> VisualLines => new(_visualLines);

	#endregion

	#region Methods

	[MemberNotNull(nameof(TextRunProperties))]
	private void EnsureTextProperties()
	{
		if (Typeface.FontFamily != FontFamily)
		{
			Typeface = new Typeface(FontFamily);
		}

		TextRunProperties = new GenericTextRunProperties(Typeface, FontSize, foregroundBrush: Foreground);
	}

	/// <summary>
	/// Clears out all visual lines and schedules a new layout pass.
	/// </summary>
	private void InvalidateVisualLines()
	{
		_visualLines.Clear();

		InvalidateArrange();
	}

	private static void OnFontRelatedPropertyChanged(FileEditor arg1, AvaloniaPropertyChangedEventArgs arg2)
	{
		arg1.EnsureTextProperties();
		arg1.InvalidateMeasure();
		arg1.InvalidateVisualLines();
	}

	#endregion
}