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

	[Display(Name = "Amber", Order = 90)]
	Amber,

	[Display(Name = "Blue", Order = 60)]
	Blue,

	[Display(Name = "Blue Gray", Order = 61)]
	BlueGray,

	[Display(Name = "Brown", Order = 120)]
	Brown,

	[Display(Name = "Deep Orange", Order = 110)]
	DeepOrange,

	[Display(Name = "Deep Purple", Order = 40)]
	DeepPurple,

	[Display(Name = "Gray", Order = 140)]
	Gray,

	[Display(Name = "Green", Order = 80)]
	Green,

	[Display(Name = "Indigo", Order = 50)]
	Indigo,

	[Display(Name = "Orange", Order = 100)]
	Orange,

	[Display(Name = "Pink", Order = 20)]
	Pink,

	[Display(Name = "Purple", Order = 30)]
	Purple,

	[Display(Name = "Red", Order = 10)]
	Red,

	[Display(Name = "Teal", Order = 70)]
	Teal
}