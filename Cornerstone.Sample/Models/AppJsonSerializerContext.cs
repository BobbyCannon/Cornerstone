#region References

using System.Text.Json.Serialization;

#endregion

namespace Cornerstone.Sample.Models;

[JsonSerializable(typeof(Account))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}