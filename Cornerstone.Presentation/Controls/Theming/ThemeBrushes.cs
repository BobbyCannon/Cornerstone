#region References

using Cornerstone.Presentation;
using Cornerstone.Presentation.Media;

#endregion

namespace Cornerstone.Presentation.Controls.Theming;

internal static class ThemeBrushes
{
	#region Methods

	public static IBrush Get(StyledElement element, string key)
	{
		// Palette entries are Color. TryGetResource returns that Color; DynamicResource
		// converts it when the target is a brush. Use the same conversion here.
		return ResourceService.GetColorAsBrush(key, 1.0, element);
	}

	#endregion
}