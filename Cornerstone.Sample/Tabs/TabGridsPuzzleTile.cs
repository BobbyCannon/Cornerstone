#region References

using System;
using Avalonia.Media.Imaging;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Sample.Tabs;

[Notifiable(["*"])]
public partial class TabGridsPuzzleTile : CornerstoneObject
{
	#region Constructors

	public TabGridsPuzzleTile(TabGrids parent)
	{
		Parent = parent;
	}

	#endregion

	#region Properties

	public partial int Column { get; set; }
	public string DisplayText => Number == 0 ? "" : Number.ToString();
	public bool IsVisible => (Number > 0) || ((Number == 0) && Parent.IsSolved);
	public partial int Number { get; set; }
	public TabGrids Parent { get; }
	public partial int Row { get; set; }
	public partial CroppedBitmap TileImage { get; set; }

	#endregion

	#region Methods

	public bool IsAdjacentTo(TabGridsPuzzleTile other)
	{
		return other is not null && ((Math.Abs(Row - other.Row) + Math.Abs(Column - other.Column)) == 1);
	}

	public void Refresh()
	{
		NotifyComputedPropertyChanged(nameof(IsVisible));
	}

	#endregion
}