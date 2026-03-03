#region References

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Avalonia.Resources;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Tabs;

[SourceReflection]
public partial class TabInkCanvas : UserControl
{
	#region Constants

	public const string HeaderName = "Ink Canvas";

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public TabInkCanvas()
	{
		Stroke = ResourceService.GetColorAsBrush("Foreground00");
		DataContext = this;

		InitializeComponent();
	}

	#endregion

	#region Properties

	[Notify]
	public partial IBrush Stroke { get; set; }

	#endregion

	#region Methods

	private void ClearOnClick(object sender, RoutedEventArgs e)
	{
		InkCanvas.Clear();
	}

	private void ColorOnClick(object sender, RoutedEventArgs e)
	{
		if (sender is not Button button)
		{
			return;
		}

		var color = Avalonia.Themes.Theme.GetColor(button);
		var brush = ResourceService.GetBrush(color);
		InkCanvas.Stroke = brush ?? ResourceService.GetColorAsBrush("Foreground00", control: button);
	}

	[RelayCommand]
	private void RemoveStroke(InkCanvasStroke stroke)
	{
		InkCanvas.History.Remove(stroke);
	}

	#endregion
}