#region References

using System.Linq;
using System.Xml;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public partial class WeaverForPropertyChanged
{
	#region Fields

	public bool CheckForEqualityUsingBaseEquals = true;

	#endregion

	#region Methods

	public void ResolveCheckForEqualityUsingBaseEqualsConfig()
	{
		var value = (ModuleWeaver.Config?.Attributes("CheckForEqualityUsingBaseEquals"))
			.Select(a => a.Value)
			.SingleOrDefault();
		if (value != null)
		{
			CheckForEqualityUsingBaseEquals = XmlConvert.ToBoolean(value.ToLowerInvariant());
		}
	}

	#endregion
}