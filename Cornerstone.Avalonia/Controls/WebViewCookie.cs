#region References

using System;
using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Avalonia.Controls;

[SourceReflection]
public partial class WebViewCookie : Notifiable
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

	#endregion
}