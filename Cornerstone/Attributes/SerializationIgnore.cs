#region References

using System;

#endregion

namespace Cornerstone.Attributes;

/// <summary>
/// Attribute for ignoring properties during serialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class SerializationIgnore : CornerstoneAttribute
{
}