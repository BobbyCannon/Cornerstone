#region References

using Sample.Shared.Storage.Sync;

#endregion

namespace Sample.Shared.Storage.Views;

public class AddressView : Address
{
	#region Constructors

	public AddressView()
	{
		IsShown = true;
		ShowUtilities = true;
		ResetHasChanges();
	}

	#endregion

	#region Properties

	public bool IsShown { get; set; }

	public bool ShowUtilities { get; set; }

	#endregion
}