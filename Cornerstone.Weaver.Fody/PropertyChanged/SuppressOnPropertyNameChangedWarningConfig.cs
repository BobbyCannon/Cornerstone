#region References

using System.Linq;
using System.Xml;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public partial class WeaverForPropertyChanged
{
	#region Fields

	public bool SuppressOnPropertyNameChangedWarning;

	#endregion

	#region Methods

	public void ResolveSuppressOnPropertyNameChangedWarningConfig()
	{
		var value = ModuleWeaver.Config.Attributes("SuppressOnPropertyNameChangedWarning")
			.Select(a => a.Value)
			.SingleOrDefault();
		if (value != null)
		{
			SuppressOnPropertyNameChangedWarning = XmlConvert.ToBoolean(value.ToLowerInvariant());
		}
	}

	#endregion
}