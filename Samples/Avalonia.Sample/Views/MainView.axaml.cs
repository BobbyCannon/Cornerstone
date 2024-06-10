#region References

using System;
using Avalonia.Input;
using Cornerstone;
using Cornerstone.Avalonia.Controls;

#endregion

namespace Avalonia.Sample.Views;

public partial class MainView : CornerstoneUserControl
{
	#region Constructors

	public MainView()
	{
		InitializeComponent();
	}

	#endregion

	#region Methods

	public void AddHistory(string message)
	{
		KeyHistory.Text += $"{TimeService.RealTime.UtcNow} : {message}{Environment.NewLine}";
	}

	private void KeyInputOnKeyDown(object sender, KeyEventArgs e)
	{
		AddHistory($"Key Down - {ToString(e)}");
	}

	private void KeyInputOnKeyUp(object sender, KeyEventArgs e)
	{
		AddHistory($"Key Up - {ToString(e)}");
	}

	private string ToString(KeyEventArgs e)
	{
		return $"{e.Key} {e.KeyModifiers} {e.KeyDeviceType} {e.KeySymbol}";
	}

	#endregion
}