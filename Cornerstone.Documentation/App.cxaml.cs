#region References

using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Documentation;

#endregion

namespace Cornerstone.Documentation;

public class App : DocumentationReaderApplication
{
	#region Methods

	public override void Initialize()
	{
		CornerstoneXamlLoader.Load(this);
		base.Initialize();
	}

	#endregion
}