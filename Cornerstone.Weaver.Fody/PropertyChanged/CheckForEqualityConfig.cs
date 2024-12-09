#region References

using System.Linq;
using System.Xml;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public partial class WeaverForPropertyChanged
{
	#region Fields

	public bool CheckForEquality = true;

	#endregion

	#region Methods

	public void ResolveCheckForEqualityConfig()
	{
		var value = (ModuleWeaver.Config?.Attributes("CheckForEquality"))
			.Select(a => a.Value)
			.SingleOrDefault();
		if (value != null)
		{
			CheckForEquality = XmlConvert.ToBoolean(value.ToLowerInvariant());
		}
	}

	#endregion
}