#region References

using System.Linq;
using System.Xml;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public partial class WeaverForPropertyChanged
{
	#region Fields

	public bool EnableIsChangedProperty = true;

	#endregion

	#region Methods

	public void ResolveEnableIsChangedPropertyConfig()
	{
		var value = (ModuleWeaver.Config?.Attributes("EnableIsChangedProperty"))
			.Select(a => a.Value)
			.SingleOrDefault();
		if (value != null)
		{
			EnableIsChangedProperty = XmlConvert.ToBoolean(value.ToLowerInvariant());
		}
	}

	#endregion
}