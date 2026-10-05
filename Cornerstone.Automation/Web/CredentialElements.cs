#region References

using Cornerstone.Data;

#endregion

namespace Cornerstone.Automation.Web;

[Notifiable(["*"])]
public partial class CredentialElements : CornerstoneObject
{
	#region Properties

	[Notify]
	public partial Elements.Input Password { get; set; }

	[Notify]
	public partial Elements.Input UserName { get; set; }

	#endregion

	#region Methods

	public void Reset()
	{
		UserName = null;
		Password = null;
	}

	#endregion
}