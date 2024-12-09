#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Fody;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public partial class WeaverForPropertyChanged
{
	#region Fields

	public List<string> EventInvokerNames =
	[
		"OnPropertyChanged",
		"SetProperty",
		"NotifyOfPropertyChange",
		"RaisePropertyChanged",
		"NotifyPropertyChanged",
		"NotifyChanged",
		"ReactiveUI.IReactiveObject.RaisePropertyChanged",
		InjectedEventInvokerName
	];

	#endregion

	#region Methods

	public void ResolveEventInvokerName()
	{
		var eventInvokerAttribute = ModuleWeaver.Config?.Attributes("EventInvokerNames").FirstOrDefault();
		if (eventInvokerAttribute == null)
		{
			return;
		}

		EventInvokerNames = eventInvokerAttribute.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
			.Select(_ => _.Trim())
			.Where(_ => _.Length > 0)
			.ToList();

		if (!EventInvokerNames.Any())
		{
			throw new WeavingException("EventInvokerNames contained no items.");
		}

		EventInvokerNames.Add(InjectedEventInvokerName);
	}

	#endregion
}