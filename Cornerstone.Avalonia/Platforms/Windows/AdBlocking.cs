#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Avalonia.Platforms.Windows;

public static class AdBlocking
{
	#region Constructors

	[SuppressMessage("ReSharper", "StringLiteralTypo")]
	static AdBlocking()
	{
		var assembly = typeof(Babel).Assembly;
		var resourceName = "Cornerstone.Resources.AdDomains.txt";
		var data = assembly.GetManifestResourceStream(resourceName).ReadString();
		AdDomains = new HashSet<string>(data.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries));

		var escaped = AdDomains.Select(Regex.Escape);
		var domains = string.Join("|", escaped);
		var pattern = $@"^(?:{domains})$|\.(?:{domains})$";
		AdRegex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
	}

	#endregion

	#region Properties

	public static HashSet<string> AdDomains { get; }

	public static Regex AdRegex { get; }

	#endregion

	#region Methods

	public static bool ShouldBlock(string uriString)
	{
		return Uri.TryCreate(uriString, UriKind.Absolute, out var uri)
			&& AdRegex.IsMatch(uri.Host);
	}

	#endregion
}