#region References

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Cornerstone.GrokMonitor.GrokUsage.Models;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage.Serialization;

[JsonSourceGenerationOptions(
	PropertyNameCaseInsensitive = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(BillingSnapshot))]
[JsonSerializable(typeof(InferenceUsage))]
[JsonSerializable(typeof(SessionInfo))]
public partial class GrokUsageJsonSerializerContext : JsonSerializerContext
{
}