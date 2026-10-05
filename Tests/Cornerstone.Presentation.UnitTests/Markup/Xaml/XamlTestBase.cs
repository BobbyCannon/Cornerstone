#nullable enable

#region References

using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml;

[SkipInAot("Runtime XAML compiles with SRE (Reflection.Emit), which Native AOT does not support.")]
public class XamlTestBase : ScopedTestBase
{
	#region Constructors

	public XamlTestBase()
	{
		if (PresentationLocator.Current.GetService<CornerstoneXamlLoader.IRuntimeXamlLoader>() == null)
		{
			PresentationLocator.CurrentMutable.Bind<CornerstoneXamlLoader.IRuntimeXamlLoader>()
				.ToConstant(new TestRuntimeXamlLoader());
		}
	}

	#endregion
}