using System;

namespace Cornerstone.Presentation.Remote.Protocol
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class PresentationRemoteMessageGuidAttribute : Attribute
    {
        public Guid Guid { get; }

        public PresentationRemoteMessageGuidAttribute(string guid)
        {
            Guid = Guid.Parse(guid);
        }
    }
}
