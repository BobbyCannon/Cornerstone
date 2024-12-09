#region References

using Cornerstone.Collections;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Extensions;

public static class SpeedyListExtensions
{
	#region Methods

	public static bool TryFind<T>(this ISpeedyList<T> list, int index, out T value) where T : TextRange
	{
		if (list.Count <= 3)
		{
			foreach (var item in list)
			{
				if ((index >= item.StartIndex) 
					&& (index < item.EndIndex))
				{
					value = item;
					return true;
				}
			}

			value = default;
			return false;
		}

		var position = list.Count / 2;
		var stepSize = position / 2;

		while (true)
		{
			if (stepSize == 0)
			{
				// Worst case found it
				if ((list[position].EndIndex >= index) 
					&& (list[position].StartIndex <= index))
				{
					value = list[position];
					return true;
				}

				value = default;
				return false;
			}

			if (list[position].EndIndex < index)
			{
				// Search down.
				position -= stepSize;
			}
			else if (list[position].StartIndex > index)
			{
				// Search up.
				position += stepSize;
			}
			else
			{
				// Found it!
				value = list[position];
				return true;
			}

			stepSize /= 2;
		}
	}

	#endregion
}