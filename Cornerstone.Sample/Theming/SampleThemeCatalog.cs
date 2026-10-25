#region References

using System;
using Cornerstone.Presentation.Theme;
using Cornerstone.Presentation.Themes.Extras.Halloween;

#endregion

namespace Cornerstone.Sample.Theming;

/// <summary>
/// Skins the sample can show. Cornerstone is the product theme. Each later demo skin is one entry here.
/// </summary>
public static class SampleThemeCatalog
{
	#region Constants

	public const string CornerstoneName = "Cornerstone";

	public const string HalloweenName = "Halloween";

	#endregion

	#region Fields

	private static readonly SampleThemeSkin[] _skins;

	#endregion

	#region Constructors

	static SampleThemeCatalog()
	{
		_skins =
		[
			new SampleThemeSkin(CornerstoneName, typeof(CornerstoneTheme), static () => new CornerstoneTheme()),
			new SampleThemeSkin(HalloweenName, typeof(HalloweenTheme), static () => new HalloweenTheme())
		];

		var names = new string[_skins.Length];
		for (var i = 0; i < _skins.Length; i++)
		{
			names[i] = _skins[i].Name;
		}

		Names = names;
	}

	#endregion

	#region Properties

	public static string[] Names { get; }

	#endregion

	#region Methods

	public static SampleThemeSkin Find(string name)
	{
		if (!string.IsNullOrEmpty(name))
		{
			for (var i = 0; i < _skins.Length; i++)
			{
				if (string.Equals(_skins[i].Name, name, StringComparison.Ordinal))
				{
					return _skins[i];
				}
			}
		}

		return _skins[0];
	}

	#endregion
}

/// <summary>
/// One selectable skin. <see cref="ThemeType" /> is the concrete theme type placed on Application.Styles.
/// </summary>
public sealed class SampleThemeSkin
{
	#region Constructors

	public SampleThemeSkin(string name, Type themeType, Func<CornerstoneTheme> create)
	{
		Name = name;
		ThemeType = themeType;
		Create = create;
	}

	#endregion

	#region Properties

	public Func<CornerstoneTheme> Create { get; }

	public string Name { get; }

	public Type ThemeType { get; }

	#endregion
}
