using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Rendering.Composition.Server;

namespace Cornerstone.Presentation.Rendering.Composition
{
    internal partial class CompositionTarget
    {
        private readonly Dictionary<object, NativeAirspaceHole> _nativeHoles = new();
        private NativeAirspaceHole[] _nativeHoleSnapshot = Array.Empty<NativeAirspaceHole>();

        internal IReadOnlyList<NativeAirspaceHole> NativeAirspaceHoles => _nativeHoleSnapshot;

        internal void PublishNativeAirspaceHole(object host, NativeAirspaceHole hole)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));
            _nativeHoles[host] = hole;
            SyncNativeAirspaceHolesToServer();
        }

        internal void RemoveNativeAirspaceHole(object host)
        {
            if (host == null || !_nativeHoles.Remove(host))
                return;
            SyncNativeAirspaceHolesToServer();
        }

        private void SyncNativeAirspaceHolesToServer()
        {
            var snapshot = new NativeAirspaceHole[_nativeHoles.Count];
            var i = 0;
            foreach (var hole in _nativeHoles.Values)
                snapshot[i++] = hole;
            Array.Sort(snapshot, (a, b) => a.TreeOrder.CompareTo(b.TreeOrder));
            _nativeHoleSnapshot = snapshot;

            if (Server is not ServerCompositionTarget server)
                return;

            Compositor.PostServerJob(() => server.SetNativeAirspaceHoles(snapshot));
        }
    }
}
