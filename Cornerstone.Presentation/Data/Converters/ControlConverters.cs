#region References

using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Media;

#endregion

namespace Cornerstone.Presentation.Data.Converters;

public static class ControlConverters
{
	#region Fields

	public static readonly FuncValueConverter<HorizontalAlignment, TextAlignment> ToTextAlignment;

	#endregion

	#region Constructors

	static ControlConverters()
	{
		ToTextAlignment = new(x =>
		{
			return x switch
			{
				HorizontalAlignment.Center => TextAlignment.Center,
				HorizontalAlignment.Right => TextAlignment.Right,
				_ => TextAlignment.Left
			};
		});
	}

	#endregion
}
