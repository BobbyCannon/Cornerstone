#region References

using System.ComponentModel.DataAnnotations;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Avalonia.Themes;

[SourceReflection]
public enum ThemeColor
{
	[Display(Order = 0)]
	None,

	[Display(Order = 1)]
	Current,

	// Generated Code - Colors

	[Display(Name = "Red", Order = 10)]
	Red,

	[Display(Name = "Deep Orange", Order = 20)]
	DeepOrange,

	[Display(Name = "Amber", Order = 30)]
	Amber,

	[Display(Name = "Orange", Order = 40)]
	Orange,

	[Display(Name = "Green", Order = 50)]
	Green,

	[Display(Name = "Teal", Order = 60)]
	Teal,

	[Display(Name = "Blue", Order = 70)]
	Blue,

	[Display(Name = "Indigo", Order = 80)]
	Indigo,

	[Display(Name = "Deep Purple", Order = 90)]
	DeepPurple,

	[Display(Name = "Purple", Order = 100)]
	Purple,

	[Display(Name = "Pink", Order = 110)]
	Pink,

	[Display(Name = "Brown", Order = 120)]
	Brown,

	[Display(Name = "Blue Gray", Order = 130)]
	BlueGray,

	[Display(Name = "Gray", Order = 140)]
	Gray

	// Generated Code - /Colors
}