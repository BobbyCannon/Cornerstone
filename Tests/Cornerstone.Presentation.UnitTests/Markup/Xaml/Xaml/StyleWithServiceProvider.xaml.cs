#nullable enable

#region References

using System;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Styling;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

public class StyleWithServiceProvider : Style
{
	#region Constructors

	public StyleWithServiceProvider(IServiceProvider? sp = null)
	{
		ServiceProvider = sp;
		CornerstoneXamlLoader.Load(sp, this);
	}

	#endregion

	#region Properties

	public IServiceProvider? ServiceProvider { get; }

	#endregion
}