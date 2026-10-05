using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Cornerstone.Presentation.Remote.Protocol
{
    public class DefaultMessageTypeResolver : IMessageTypeResolver
    {
        private readonly Dictionary<Guid, Type> _guidsToTypes = new Dictionary<Guid, Type>();
        private readonly Dictionary<Type, Guid> _typesToGuids = new Dictionary<Type, Guid>();

        [UnconditionalSuppressMessage("Trimming", "IL2026",
            Justification = "If type was trimmed, we don't need to resolve it in the remove protocol")]
        public DefaultMessageTypeResolver(params Assembly[] assemblies)
        {
            foreach (var asm in
                (assemblies ?? Array.Empty<Assembly>()).Concat(new[]
                    {typeof(PresentationRemoteMessageGuidAttribute).GetTypeInfo().Assembly}))
            {
                foreach (var t in asm.ExportedTypes)
                {
                    var attr = t.GetTypeInfo().GetCustomAttribute<PresentationRemoteMessageGuidAttribute>();
                    if (attr != null)
                    {
                        _guidsToTypes[attr.Guid] = t;
                        _typesToGuids[t] = attr.Guid;
                    }
                }
            }
        }

        public Type GetByGuid(Guid id) => _guidsToTypes[id];
        public Guid GetGuid(Type type) => _typesToGuids[type];
    }
}
