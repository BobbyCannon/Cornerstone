#region References

using System;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Theme;
using Cornerstone.Presentation.Theme.Theming;

#endregion

namespace Cornerstone.Presentation.Themes.Extras.Halloween;

/// <summary>
/// Purple night and pumpkin orange, using the product control templates.
/// Colors follow the Halloween treatment in Larry's holiday page.
/// </summary>
public class HalloweenTheme : CornerstoneTheme
{
	#region Constructors

	public HalloweenTheme() : this(null!)
	{
	}

	public HalloweenTheme(IServiceProvider sp) : base(sp)
	{
		ThemeColor = ThemeColor.DeepOrange;
	}

	#endregion

	#region Methods

	protected override void LoadThemeXaml(IServiceProvider sp)
	{
		CornerstoneXamlLoader.Load(sp, this);
	}

	#endregion
}