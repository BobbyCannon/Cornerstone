#region References

using System.Linq;
using System.Xml;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public partial class WeaverForPropertyChanged
{
	#region Fields

	public bool UseStaticEqualsFromBase;

	#endregion

	#region Methods

	public void ResolveUseStaticEqualsFromBaseConfig()
	{
		var value = (ModuleWeaver.Config?.Attributes("UseStaticEqualsFromBase"))
			.Select(a => a.Value)
			.SingleOrDefault();
		if (value != null)
		{
			UseStaticEqualsFromBase = XmlConvert.ToBoolean(value.ToLowerInvariant());
		}
	}

	#endregion
}