#region References

using Cornerstone.Presentation.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

[TestClass]
public class TestPlatformSettings(PlatformThemeVariant themeVariant)
	: DefaultPlatformSettings
{
	#region Fields

	private PlatformColorValues _colorValues = new() { ThemeVariant = themeVariant };

	#endregion

	#region Properties

	public PlatformThemeVariant ThemeVariant
	{
		get => _colorValues.ThemeVariant;
		set => SetColorValues(_colorValues with { ThemeVariant = value });
	}

	#endregion

	#region Methods

	public override PlatformColorValues GetColorValues()
	{
		return _colorValues;
	}

	public void SetColorValues(PlatformColorValues colorValues)
	{
		_colorValues = colorValues;
		OnColorValuesChanged(colorValues);
	}

	#endregion
}