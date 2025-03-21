#region References

using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Media;
using Cornerstone.Avalonia.Resources;

#endregion

namespace Cornerstone.Avalonia.Converters;

public static class ResourceConverters
{
	#region Fields

	public static readonly FuncValueConverter<bool, string, IBrush> GetBrush;
	public static readonly FuncValueConverter<string, StreamGeometry> GetSvg;
	public static readonly FuncValueConverter<string, KeyGesture> ToKeyGesture;

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
		ToKeyGesture = new FuncValueConverter<string, KeyGesture>(x => string.IsNullOrWhiteSpace(x) ? null : KeyGesture.Parse(x));
	}

	#endregion

	#region Methods

	private static string TryGet(string[] values, int offset, string defaultValue)
	{
		return offset < values?.Length ? values[offset] : defaultValue;
	}

	#endregion
}