using System;

namespace Cornerstone.Presentation.Metadata
{
    /// <summary>
    /// Defines the ambient class/property
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = true)]
    public sealed class AmbientAttribute : Attribute
    {
    }
}
