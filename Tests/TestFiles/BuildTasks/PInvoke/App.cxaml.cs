#region References

using Cornerstone.Presentation;
using Cornerstone.Presentation.Markup.Xaml;

#endregion

namespace PInvoke;

public partial class App : Application
{
	#region Methods

	public override void Initialize()
	{
		CornerstoneXamlLoader.Load(this);
	}

	#endregion
}