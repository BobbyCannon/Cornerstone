#region References

using System.Linq;
using System.Xml;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public partial class WeaverForPropertyChanged
{
	#region Fields

	public bool InjectOnPropertyNameChanged = true;
	public bool InjectOnPropertyNameChanging = true;

	#endregion

	#region Methods

	public void ResolveOnPropertyNameChangedConfig()
	{
		var value = (ModuleWeaver.Config?.Attributes("InjectOnPropertyNameChanged"))
			.Select(a => a.Value)
			.SingleOrDefault();
		if (value != null)
		{
			InjectOnPropertyNameChanged = XmlConvert.ToBoolean(value.ToLowerInvariant());
		}
	}

	public void ResolveOnPropertyNameChangingConfig()
	{
		var value = ModuleWeaver.Config?.Attributes("InjectOnPropertyNameChanging").FirstOrDefault();
		if (value != null)
		{
			InjectOnPropertyNameChanging = bool.Parse((string) value);
		}
	}

	#endregion
}