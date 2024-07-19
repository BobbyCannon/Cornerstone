#region References

using System.ComponentModel.DataAnnotations;

#endregion

namespace Cornerstone.Avalonia;

public enum ThemeColor
{
	[Display(Order = 0)]
	Default,

	[Display(Order = 1)]
	Current,

	[Display(Order = 90)]
	Amber,

	[Display(Order = 60)]
	Blue,

	[Display(Order = 130)]
	BlueGrey,

	[Display(Order = 120)]
	Brown,

	[Display(Order = 110)]
	DeepOrange,

	[Display(Order = 40)]
	DeepPurple,

	[Display(Order = 80)]
	Green,

	[Display(Order = 50)]
	Indigo,

	[Display(Order = 100)]
	Orange,

	[Display(Order = 20)]
	Pink,

	[Display(Order = 30)]
	Purple,

	[Display(Order = 10)]
	Red,

	[Display(Order = 70)]
	Teal
}