#region References

using Avalonia.Markup.Xaml;
using Avalonia.Styling;

#endregion

namespace Cornerstone.VisualStudio.Tests;

public class TestCompiledTheme : Styles
{
	#region Constructors

	public TestCompiledTheme()
	{
		AvaloniaXamlLoader.Load(this);
	}

	#endregion
}