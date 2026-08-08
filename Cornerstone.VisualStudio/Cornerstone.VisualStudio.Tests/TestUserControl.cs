#region References

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

#endregion

namespace Cornerstone.VisualStudio.Tests;

public class TestUserControl : UserControl
{
	#region Constructors

	public TestUserControl()
	{
		AvaloniaXamlLoader.Load(this);
	}

	#endregion
}