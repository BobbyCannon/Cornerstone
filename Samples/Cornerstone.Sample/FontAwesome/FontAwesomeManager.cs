#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Cornerstone.Avalonia;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Sample.FontAwesome;

public class FontAwesomeManager : Notifiable
{
	#region Fields

	private static readonly Regex _propertyRegEx = new(@"\([^)]*\)");

	#endregion

	#region Properties

	public Dictionary<string, string> CssNameByUnicode { get; private set; }

	public Dictionary<string, FontAwesomeIcon> IconLookup { get; private set; }

	public SpeedyList<FontAwesomeIcon> Icons { get; private set; }

	#endregion

	#region Methods

	public string CleanupMember(string text)
	{
		var cultureInfo = Thread.CurrentThread.CurrentCulture;
		var textInfo = cultureInfo.TextInfo;
		var stringBuilder = new StringBuilder(textInfo.ToTitleCase(text.Replace("-", " ")));

		stringBuilder
			.Replace("-", string.Empty).Replace("/", "_")
			.Replace(" ", string.Empty).Replace(".", string.Empty)
			.Replace("'", string.Empty);

		var matches = _propertyRegEx.Matches(stringBuilder.ToString());
		stringBuilder = new StringBuilder(_propertyRegEx.Replace(stringBuilder.ToString(), string.Empty));
		var hasMatch = false;

		for (var i = 0; i < matches.Count; i++)
		{
			var match = matches[i];
			if (match.Value.IndexOf("Hand", StringComparison.InvariantCultureIgnoreCase) > -1)
			{
				hasMatch = true;
				break;
			}
		}

		if (hasMatch)
		{
			stringBuilder.Insert(0, "Hand");
		}

		if (char.IsDigit(stringBuilder[0]))
		{
			stringBuilder.Insert(0, '_');
		}

		if (stringBuilder.ToString() == "Equals")
		{
			stringBuilder.Append("Symbol");
		}

		return stringBuilder.ToString();
	}

	public async Task LoadAsync(string iconsJsonFilePath)
	{
		var json = await File.ReadAllTextAsync(iconsJsonFilePath);
		var dictionary = json.FromJson<Dictionary<string, FontAwesomeIcon>>();

		if (Design.IsDesignMode)
		{
			var keys = dictionary.Keys.Skip(100).ToList();
			keys.ForEach(x => dictionary.Remove(x));
		}

		await CornerstoneDispatcher
			.Instance
			.DispatchAsync(() =>
			{
				IconLookup = dictionary;
				CssNameByUnicode = IconLookup?.ToDictionary(x => $"{x.Value.Unicode}", x => $"fa-{x.Key}", StringComparer.OrdinalIgnoreCase);
				Icons = new SpeedyList<FontAwesomeIcon>(null, new OrderBy<FontAwesomeIcon>(x => x.Label))
				{
					FilterCheck = x => x.IsVisible
				};
				Icons.Add(IconLookup?.Values);
			});
	}

	#endregion
}

public class IconSvgCollection : Notifiable
{
	#region Properties

	public IconSvg Brands { get; set; }

	//public IconSvg DuoTone { get; set; }

	public IconSvg Light { get; set; }

	public IconSvg Regular { get; set; }

	public IconSvg Solid { get; set; }

	#endregion

	#region Methods

	public IEnumerable<IconSvg> GetSvgs()
	{
		if (Brands != null)
		{
			yield return Brands;
		}

		if (Solid != null)
		{
			yield return Solid;
		}

		if (Light != null)
		{
			yield return Light;
		}

		//if (DuoTone != null)
		//{
		//	yield return DuoTone;
		//}

		if (Regular != null)
		{
			yield return Regular;
		}
	}

	#endregion
}

public class IconSvg : Notifiable
{
	#region Properties

	public string Path { get; set; }

	#endregion
}

public class IconSearch : Notifiable
{
	#region Properties

	public string[] Terms { get; set; }

	#endregion
}