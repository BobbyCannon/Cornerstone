#region References

using System.Text.Json.Serialization;

#endregion

namespace Cornerstone.Sync;

[JsonSerializable(typeof(SyncIssue))]
[JsonSerializable(typeof(SyncObject))]
[JsonSerializable(typeof(SyncRequest))]
[JsonSerializable(typeof(SyncSettings))]
[JsonSerializable(typeof(SyncSession))]
[JsonSerializable(typeof(SyncStatistics))]
[JsonSerializable(typeof(SyncTimes))]
public partial class SyncSerializerContext : JsonSerializerContext
{
}