using System;
using System.Collections.Generic;
using Cornerstone.Presentation.VisualTree;

namespace Cornerstone.Presentation.Platform
{
    /// <summary>
    /// Native islands sit under the Skia plane with transparent holes.
    /// Set from platform options at windowing init. See Presentation/NativeLayering.md.
    /// </summary>
    internal static class NativeAirspace
    {
        private static readonly object Sync = new();
        private static readonly Dictionary<object, int> AttachedHosts = new();
        private static readonly Dictionary<object, NativeAirspaceHole[]> HolesByOwner = new();
        private static readonly NativeAirspaceHole[] NoHoles = Array.Empty<NativeAirspaceHole>();

        public static bool BehindComposition { get; set; }

        public static bool IsHoleHit(object hit) => hit is Controls.NativeControlHost;

        /// <summary>
        /// True when the hit belongs to Cornerstone chrome over a hole.
        /// The host, its descendants, and ancestors of a host stay with native.
        /// </summary>
        public static bool IsInteractiveOverlay(Visual hit, Visual root)
        {
            if (!IsOverlayCandidate(hit) || root == null)
                return false;
            foreach (var descendant in root.GetVisualDescendants())
            {
                if (descendant is Controls.NativeControlHost host
                    && host.IsEffectivelyVisible
                    && hit.IsVisualAncestorOf(host))
                    return false;
            }

            return true;
        }

        public static bool IsInteractiveOverlay(Visual hit, IReadOnlyList<Controls.NativeControlHost> hosts)
        {
            if (!IsOverlayCandidate(hit))
                return false;
            if (hosts == null)
                return true;
            for (var i = 0; i < hosts.Count; i++)
            {
                var host = hosts[i];
                if (host.IsEffectivelyVisible && hit.IsVisualAncestorOf(host))
                    return false;
            }

            return true;
        }

        private static bool IsOverlayCandidate(Visual hit)
        {
            if (hit == null)
                return false;
            return hit.FindAncestorOfType<Controls.NativeControlHost>(includeSelf: true) == null;
        }

        public static void AddAttachedHost(object owner)
        {
            lock (Sync)
            {
                AttachedHosts.TryGetValue(owner, out var count);
                AttachedHosts[owner] = count + 1;
            }
        }

        public static void RemoveAttachedHost(object owner)
        {
            lock (Sync)
            {
                if (!AttachedHosts.TryGetValue(owner, out var count))
                    return;
                if (count <= 1)
                    AttachedHosts.Remove(owner);
                else
                    AttachedHosts[owner] = count - 1;
            }
        }

        public static bool HasAttachedHost(object owner)
        {
            lock (Sync)
                return AttachedHosts.TryGetValue(owner, out var count) && count > 0;
        }

        public static void SetHoles(object owner, IReadOnlyList<NativeAirspaceHole> holes)
        {
            lock (Sync)
            {
                if (holes == null || holes.Count == 0)
                {
                    HolesByOwner.Remove(owner);
                    return;
                }

                var copy = new NativeAirspaceHole[holes.Count];
                for (var i = 0; i < holes.Count; i++)
                    copy[i] = holes[i];
                HolesByOwner[owner] = copy;
            }
        }

        public static NativeAirspaceHole[] GetHoles(object owner)
        {
            lock (Sync)
                return HolesByOwner.TryGetValue(owner, out var holes) ? holes : NoHoles;
        }
    }

    /// <summary>
    /// PlatformImpl that can punch native-airspace holes out of the window's
    /// input region (Wayland <c>wl_surface.set_input_region</c>).
    /// </summary>
    internal interface INativeAirspaceInputHost
    {
        void SyncNativeAirspaceInput();
    }
}
