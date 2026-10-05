#region References

using System;
using System.Collections.Generic;
using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Presentation.Controls;

#endregion

namespace Cornerstone.Presentation.Controls.Web;

[SourceReflection]
public partial class WebViewCookie : CornerstoneObject
{
	#region Properties

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial string Domain { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial DateTime Expires { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial bool IsHostOnly { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial bool IsHttpOnly { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial bool IsSecure { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial bool IsSession { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial string Name { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial string Path { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial WebViewCookieSameSite SameSite { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial string Value { get; set; }

	public string ExpirationDisplay
	{
		get
		{
			if (IsSession || (Expires == default))
			{
				return "No Expiration";
			}

			return Expires.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
		}
	}

	public string FlagsDisplay
	{
		get
		{
			var flags = new List<string>();
			if (IsSecure)
			{
				flags.Add("Secure");
			}

			if (IsHttpOnly)
			{
				flags.Add("HttpOnly");
			}

			if (IsHostOnly)
			{
				flags.Add("Host only");
			}

			return flags.Count == 0 ? string.Empty : string.Join(", ", flags);
		}
	}

	#endregion
}