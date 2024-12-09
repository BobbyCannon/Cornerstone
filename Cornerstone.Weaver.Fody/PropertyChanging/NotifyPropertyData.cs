#region References

using System.Collections.Generic;
using Mono.Cecil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanging;

public class NotifyPropertyData
{
	#region Fields

	public List<PropertyDefinition> AlsoNotifyFor;

	#endregion

	#region Constructors

	public NotifyPropertyData()
	{
		AlsoNotifyFor = new();
	}

	#endregion
}