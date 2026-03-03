#region References

using System.Linq;
using Avalonia.Controls;
using Avalonia.Data.Converters;

#endregion

namespace Cornerstone.Avalonia.DockingManager;

public static class DockingManagerConverters
{
	#region Fields

	public static readonly FuncMultiValueConverter<object, bool> ToTabSeparatorVisible =
		new(v =>
		{
			if (v == null)
			{
				return false;
			}

			var array = v.ToList();
			if (array.Count < 3)
			{
				return false;
			}

			if (array[0] is not DockableTabModel self
				|| array[1] is not DockableTabView selected
				|| array[2] is not ItemCollection collections)
			{
				return false;
			}

			var modelTab = collections.Cast<DockableTabView>().FirstOrDefault(x => x.TabModel == self);
			var tabIndex = collections.IndexOf(modelTab);
			var selectedIndex = collections.IndexOf(selected);
			var isFirstTab = tabIndex == 0;
			var isCurrentTab = self == selected.TabModel;
			var isNextTab = tabIndex == (selectedIndex + 1);

			return !(isFirstTab || isCurrentTab || isNextTab);
		});

	#endregion
}