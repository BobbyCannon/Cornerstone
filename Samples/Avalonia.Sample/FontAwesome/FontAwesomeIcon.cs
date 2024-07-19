#region References

using System.Collections.Generic;
using Cornerstone.Data;

#endregion

namespace Avalonia.Sample.FontAwesome;

public class FontAwesomeIcon : Notifiable
{
	#region Properties

	public bool IsVisible { get; set; }

	public string Label { get; set; }

	public IconSearch Search { get; set; }

	public List<string> Styles { get; set; }

	public IconSvgCollection Svg { get; set; }

	public string Unicode { get; set; }

	#endregion
}