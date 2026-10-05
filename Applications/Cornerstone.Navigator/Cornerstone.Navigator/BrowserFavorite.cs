#region References

using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Navigator;

[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"], false)]
public partial class BrowserFavorite : CornerstoneObject
{
	#region Constructors

	public BrowserFavorite()
	{
		Name = string.Empty;
		Uri = string.Empty;
	}

	public BrowserFavorite(string name, string uri)
	{
		Name = name ?? string.Empty;
		Uri = uri ?? string.Empty;
	}

	#endregion

	#region Properties

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial string Name { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial string Uri { get; set; }

	#endregion
}