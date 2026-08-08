#region References

using System;
using System.Text.Json.Serialization;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Serialization.Json;

[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(int[]))]
[JsonSerializable(typeof(WindowLocation))]
public partial class CornerstoneJsonSerializerContext : JsonSerializerContext
{
}