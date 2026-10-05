#region References

using Cornerstone.Presentation.Input;

#endregion

namespace Cornerstone.Presentation.Data.Converters;

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
