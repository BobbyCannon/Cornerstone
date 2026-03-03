#region References

using Avalonia.Data.Converters;
using Avalonia.Input;

#endregion

namespace Cornerstone.Avalonia.Converters;

public static class KeyGestureConverters
{
	#region Fields

	public static readonly FuncValueConverter<string, KeyGesture> ToKeyGesture;

	#endregion

	#region Constructors

	static KeyGestureConverters()
	{
		ToKeyGesture = new(x =>
		{
			if (x == null)
			{
				return null;
			}

			try
			{
				return KeyGesture.Parse(x);
			}
			catch
			{
				return null;
			}
		});
	}

	#endregion
}