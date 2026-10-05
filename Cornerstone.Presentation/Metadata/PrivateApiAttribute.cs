using System;

namespace Cornerstone.Presentation.Metadata;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Enum
                | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Struct)]
public sealed class PrivateApiAttribute : Attribute
{

}