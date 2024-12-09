#region References

using System.Collections.Generic;
using Mono.Cecil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public class NotifyPropertyData
{
	#region Fields

	public List<PropertyDefinition> AlsoNotifyFor = new();

	#endregion
}