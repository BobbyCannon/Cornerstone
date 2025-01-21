#region References

using Cornerstone.Serialization.Json.Values;

#endregion

namespace Cornerstone.Serialization.Json.Converters;

public interface IJsonParseable
{
	#region Methods

	void Parse(JsonObject jsonObject);

	#endregion
}