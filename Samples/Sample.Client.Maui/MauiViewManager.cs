#region References

using Cornerstone.Maui;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Sample.Client.Maui.Pages;
using Sample.Shared;

#endregion

namespace Sample.Client.Maui;

public class MauiViewManager : SharedViewModel
{
	#region Constructors

	public MauiViewManager(IRuntimeInformation runtimeInformation, MauiLocationProvider locationProvider, IDispatcher dispatcher)
		: base(runtimeInformation, dispatcher)
	{
		PageSwitcher = new PageSwitcher(this, dispatcher);
	}

	#endregion

	#region Properties

	public PageSwitcher PageSwitcher { get; }

	#endregion
}