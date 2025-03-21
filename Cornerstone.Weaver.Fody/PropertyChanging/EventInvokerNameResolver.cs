#region References

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanging;

public partial class WeaverForPropertyChanging
{
	#region Fields

	public List<string> EventInvokerNames =
	[
		"OnPropertyChanging",
		"raisePropertyChanging"
	];

	#endregion

	#region Methods

	public void ResolveEventInvokerName()
	{
		var eventInvokerAttribute = ModuleWeaver.Config.Attributes("EventInvokerNames").FirstOrDefault();
		if (eventInvokerAttribute != null)
		{
			EventInvokerNames.InsertRange(0,
				eventInvokerAttribute.Value
					.Split([','], StringSplitOptions.RemoveEmptyEntries)
					.Select(_ => _.Trim())
					.Where(_ => _.Length > 0)
					.ToList()
			);
		}
	}

	#endregion
}