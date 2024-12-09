#region References

using System.Linq;
using System.Xml;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public partial class WeaverForPropertyChanged
{
	#region Fields

	public bool SuppressWarnings;

	#endregion

	#region Methods

	public void ResolveSuppressWarningsConfig()
	{
		var value = (ModuleWeaver.Config?.Attributes("SuppressWarnings"))
			.Select(a => a.Value)
			.SingleOrDefault();
		if (value != null)
		{
			SuppressWarnings = XmlConvert.ToBoolean(value.ToLowerInvariant());
		}
	}

	#endregion
}