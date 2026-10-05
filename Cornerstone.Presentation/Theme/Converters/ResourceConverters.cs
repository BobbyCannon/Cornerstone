#region References

using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Presentation.Theme.Converters;

public static class ResourceConverters
{
	#region Fields

	public static readonly FuncValueConverter<bool, string, IBrush> GetBrush;
	public static readonly FuncValueConverter<string, StreamGeometry> GetSvg;

	#endregion

	#region Constructors

	static ResourceConverters()
	{
		GetBrush = new((x, p) =>
		{
			var values = p?.Split(";");
			var defaultValue = TryGet(values, 2, "Background06");
			var value = x
				? ResourceService.GetColorAsBrush(TryGet(values, 0, defaultValue))
				: ResourceService.GetColorAsBrush(TryGet(values, 1, defaultValue));
			return value;
		});

		GetSvg = new FuncValueConverter<string, StreamGeometry>(ResourceService.GetSvg);
	}

	#endregion

	#region Methods

	private static string TryGet(string[] values, int offset, string defaultValue)
	{
		return offset < values?.Length ? values[offset] : defaultValue;
	}

	#endregion
}