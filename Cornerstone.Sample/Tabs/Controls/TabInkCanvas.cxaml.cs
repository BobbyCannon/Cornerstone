#region References

using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Media;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Input;
using Cornerstone.Presentation.Controls.Ink;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Sample.Tabs.Controls;

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

		var color = Presentation.Theme.Theming.Theme.GetColor(button);
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
