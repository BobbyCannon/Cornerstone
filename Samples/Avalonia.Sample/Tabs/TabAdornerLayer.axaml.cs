#region References

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Cornerstone.Avalonia.Controls;

#endregion

namespace Avalonia.Sample.Tabs;

public partial class TabAdornerLayer : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "AdornerLayer";

	#endregion

	#region Fields

	private Control _adorner;

	#endregion

	#region Constructors

	public TabAdornerLayer()
	{
		InitializeComponent();
	}

	#endregion

	#region Methods

	// Add adorner button click event handler.

	private void AddAdorner_OnClick(object sender, RoutedEventArgs e)
	{
		var adornerButton = this.FindControl<Button>("AdornerButton");

		if (adornerButton is null)
		{
			return;
		}

		if (_adorner is not null)
		{
			AdornerLayer.SetAdorner(adornerButton, _adorner);
		}
	}

	// Remove adorner button click event handler.

	private void RemoveAdorner_OnClick(object sender, RoutedEventArgs e)
	{
		var adornerButton = this.FindControl<Button>("AdornerButton");

		if (adornerButton is null)
		{
			return;
		}

		var adorner = AdornerLayer.GetAdorner(adornerButton);

		if (adorner is not null)
		{
			_adorner = adorner;
		}

		AdornerLayer.SetAdorner(adornerButton, null);
	}

	#endregion
}