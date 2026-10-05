#region References

using System.Collections.Generic;
using System.Text.Json;

#endregion

namespace Cornerstone.Agent.Tools;

public static class ToolExtensions
{
	#region Methods

	public static string GetStringParameter(this Dictionary<string, object> parameters, string key)
	{
		if (!parameters.TryGetValue(key, out var val))
		{
			return null;
		}

		if (val is string str)
		{
			return str;
		}

		return val is JsonElement elem
			? elem.ValueKind == JsonValueKind.String
				? elem.GetString()
				: elem.GetRawText()
			: val?.ToString();
	}

	public static bool TryGetProperty(this Dictionary<string, object> parameters, string key, out string value)
	{
		if (!parameters.TryGetValue(key, out var val))
		{
			value = null;
			return false;
		}

		if (val is string str)
		{
			value = str;
			return true;
		}

		value = val is JsonElement elem
			? elem.ValueKind == JsonValueKind.String
				? elem.GetString()
				: elem.GetRawText()
			: val?.ToString();

		return true;
	}

	#endregion
}