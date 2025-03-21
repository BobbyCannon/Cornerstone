#region References

using Cornerstone.Collections;
using Sample.Shared.Storage.Sync;

#endregion

namespace Sample.Shared.Storage.Views;

public class AccountView : Account
{
	#region Constructors

	public AccountView()
	{
		Addresses = new SpeedyList<AddressView>(null, new OrderBy<AddressView>(x => x.Line1));
		IsShown = true;
		ShowChildren = true;
		ResetHasChanges();
	}

	#endregion

	#region Properties

	public SpeedyList<AddressView> Addresses { get; }

	public bool IsShown { get; set; }

	public bool ShowChildren { get; set; }

	#endregion
}