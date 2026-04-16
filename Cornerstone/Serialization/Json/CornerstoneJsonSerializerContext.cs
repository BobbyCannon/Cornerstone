#region References

using System.Text.Json.Serialization;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Serialization.Json;

[JsonSerializable(typeof(WindowLocation))]
public partial class CornerstoneJsonSerializerContext : JsonSerializerContext
{
}