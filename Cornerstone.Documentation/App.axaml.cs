#region References

using Avalonia.Markup.Xaml;
using Cornerstone.Avalonia.Documentation;

#endregion

namespace Cornerstone.Documentation;

public class App : DocumentationReaderApplication
{
	#region Methods

	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
		base.Initialize();
	}

	#endregion
}