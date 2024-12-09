#region References

using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public partial class WeaverForPropertyChanged
{
	#region Properties

	public List<string> NamespaceFilters { get; private set; }

	#endregion

	#region Methods

	public void ProcessFilterTypeAttributes()
	{
		ReadFilterTypeData(ModuleDefinition);
	}

	public void ReadFilterTypeData(ModuleDefinition moduleDefinition)
	{
		var filterTypeAttribute = moduleDefinition.Assembly.CustomAttributes.GetAttributes("Cornerstone.Weaver.FilterTypeAttribute");
		if (filterTypeAttribute == null)
		{
			return;
		}
		var customAttributeArguments = filterTypeAttribute.Select(_ => _.ConstructorArguments).ToList();
		NamespaceFilters = customAttributeArguments.Select(x => (string) x[0].Value).ToList();
	}

	#endregion
}