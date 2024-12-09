#region References

using System;
using Avalonia.Controls;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Controls;

#endregion

namespace Avalonia.Sample.Tabs;

public partial class TabButtonSpinner : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "ButtonSpinner";

	#endregion

	#region Fields

	// Private variables.

	private readonly string[] _mountains =
	{
		"Apple",
		"Banana",
		"Blueberry",
		"Cherry",
		"Grape",
		"Orange",
		"Mango",
		"Raspberry",
		"Watermelon"
	};

	#endregion

	#region Constructors

	// Constructor.

	public TabButtonSpinner()
	{
		InitializeComponent();
	}

	#endregion

	#region Methods

	// Spin button click event handler.

	public void OnSpin(object sender, SpinEventArgs e)
	{
		var spinner = (ButtonSpinner) sender;

		if (spinner.Content is not TextBlock txtBox)
		{
			return;
		}

		var value = Array.IndexOf(_mountains, txtBox.Text);

		if (e.Direction == SpinDirection.Increase)
		{
			value++;
		}
		else
		{
			value--;
		}

		if (value < 0)
		{
			value = _mountains.Length - 1;
		}
		else if (value >= _mountains.Length)
		{
			value = 0;
		}

		txtBox.Text = _mountains[value];
	}

	#endregion
}