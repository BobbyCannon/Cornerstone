#region References

using System.Text.Json.Serialization;
using Cornerstone.Agent.Keystone.State;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Agent.Serialization;

[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(PresentationList<string>))]
public partial class AppSerializerContext : JsonSerializerContext
{
}