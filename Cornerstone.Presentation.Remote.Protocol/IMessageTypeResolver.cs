using System;

namespace Cornerstone.Presentation.Remote.Protocol
{
    public interface IMessageTypeResolver
    {
        Type GetByGuid(Guid id);
        Guid GetGuid(Type type);
    }
}