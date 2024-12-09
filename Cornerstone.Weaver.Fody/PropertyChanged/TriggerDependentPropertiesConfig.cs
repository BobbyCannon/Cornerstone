#region References

using System.Linq;
using System.Xml;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public partial class WeaverForPropertyChanged
{
	#region Fields

	public bool TriggerDependentProperties = true;

	#endregion

	#region Methods

	public void ResolveTriggerDependentPropertiesConfig()
	{
		var value = ModuleWeaver.Config.Attributes("TriggerDependentProperties")
			.Select(_ => _.Value)
			.SingleOrDefault();
		if (value != null)
		{
			TriggerDependentProperties = XmlConvert.ToBoolean(value.ToLowerInvariant());
		}
	}

	#endregion
}