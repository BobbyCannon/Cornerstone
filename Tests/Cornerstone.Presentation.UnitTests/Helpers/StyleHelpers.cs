#region References

using Cornerstone.Presentation.PropertyStore;
using Cornerstone.Presentation.Styling;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

public static class StyleHelpers
{
	#region Methods

	public static void TryAttach(Style style, StyledElement element, object host = null)
	{
		style.TryAttach(element, host ?? element, FrameType.Style);
	}

	#endregion
}