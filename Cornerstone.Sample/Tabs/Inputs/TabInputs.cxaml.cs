#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Reflection;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Sample.Tabs.Inputs;

[SourceReflection]
public partial class TabInputs : UserControl
{
	#region Constants

	public const string HeaderName = "Inputs";

	#endregion

	#region Constructors

	public TabInputs()
	{
		InitializeComponent();
	}

	#endregion

	#region Methods

	private void Open(string title, Control page)
	{
		PageNavigator.GetPageNavigator(this).Navigate(title, page);
	}

	private void OpenGamepad(object sender, RoutedEventArgs e)
	{
		Open(TabInputsGamepad.HeaderName, new TabInputsGamepad());
	}

	private void OpenKeyboardMouse(object sender, RoutedEventArgs e)
	{
		Open(TabInputsKeyboardMouse.HeaderName, new TabInputsKeyboardMouse());
	}

	#endregion
}
