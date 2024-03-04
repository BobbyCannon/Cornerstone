#region References

using Cornerstone.Text;
using Newtonsoft.Json.Serialization;

#endregion

namespace Cornerstone.Newtonsoft;

/// <summary>
/// The contract resolver.
/// </summary>
public class JsonContractResolver : DefaultContractResolver
{
	#region Constructors

	/// <summary>
	/// Initializes a contract resolver for serializing.
	/// </summary>
	/// <param name="convention"> The naming convention to use during serialization. </param>
	public JsonContractResolver(NamingConvention convention)
	{
		NamingStrategy = convention == NamingConvention.CamelCase
			? new CamelCaseNamingStrategy()
			: new DefaultNamingStrategy();

		NamingStrategy.OverrideSpecifiedNames = false;
		NamingStrategy.ProcessDictionaryKeys = true;
		NamingStrategy.ProcessExtensionDataNames = false;
	}

	#endregion
}