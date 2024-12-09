#region References

using System.Linq;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanging;

public partial class WeaverForPropertyChanging
{
	#region Fields

	public bool InjectOnPropertyNameChanging = true;

	#endregion

	#region Methods

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