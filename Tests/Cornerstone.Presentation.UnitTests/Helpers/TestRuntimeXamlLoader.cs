#region References

using Cornerstone.Presentation.Markup.Xaml;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

internal sealed class TestRuntimeXamlLoader : CornerstoneXamlLoader.IRuntimeXamlLoader
{
	#region Methods

	public object Load(RuntimeXamlLoaderDocument document, RuntimeXamlLoaderConfiguration configuration)
	{
		return CornerstoneXamlIlRuntimeLoader.Load(document, configuration);
	}

	#endregion
}