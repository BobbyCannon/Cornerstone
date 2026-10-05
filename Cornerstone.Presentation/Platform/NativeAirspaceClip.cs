using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.VisualTree;

namespace Cornerstone.Presentation.Platform
{
    /// <summary>
    /// Clipped AABB for a native host: ancestor Clip / ClipToBounds / window client.
    /// Same rect is used for ShowInBounds. Skia/portal hole uses GetHoleCornerRadius.
    /// </summary>
    internal static class NativeAirspaceClip
    {
        public static Rect Intersect(Rect a, Rect b)
        {
            if (a.IsEmpty() || b.IsEmpty())
                return default;
            return a.Intersect(b);
        }

        public static Rect? GetClippedAbsoluteBounds(Layoutable host, Visual root)
        {
            var unclipped = TransformLocalRectToRoot(host, new Rect(host.Bounds.Size), root, host);
            if (unclipped == null)
                return null;

            var clipped = unclipped.Value;
            clipped = Intersect(clipped, new Rect(root.Bounds.Size));
            if (clipped.IsEmpty())
                return default(Rect);

            for (Visual visual = host; visual != null && visual != root; visual = visual.GetVisualParent())
            {
                if (visual.ClipToBounds)
                {
                    var ancestor = TransformLocalRectToRoot(visual, new Rect(visual.Bounds.Size), root, host);
                    if (ancestor == null)
                        return null;
                    clipped = Intersect(clipped, ancestor.Value);
                    if (clipped.IsEmpty())
                        return default(Rect);
                }

                if (visual.Clip != null)
                {
                    var clip = TransformLocalRectToRoot(visual, visual.Clip.Bounds, root, host);
                    if (clip == null)
                        return null;
                    clipped = Intersect(clipped, clip.Value);
                    if (clipped.IsEmpty())
                        return default(Rect);
                }
            }

            return clipped;
        }

        public static int GetTreeOrder(Visual host, Visual root)
        {
            var order = 0;
            return Walk(root, host, ref order) ? order : 0;
        }

        private static bool Walk(Visual current, Visual target, ref int order)
        {
            if (ReferenceEquals(current, target))
                return true;

            foreach (var child in current.GetVisualChildren())
            {
                order++;
                if (Walk(child, target, ref order))
                    return true;
            }

            return false;
        }

        private static Rect? TransformLocalRectToRoot(Visual from, Rect local, Visual root, Layoutable roundingHost)
        {
            var transformToVisual = from.TransformToVisual(root);
            if (transformToVisual == null)
                return null;

            var transformedRect = local.TransformToAABB(transformToVisual.Value);
            if (!roundingHost.UseLayoutRounding)
                return transformedRect;

            var scale = LayoutHelper.GetLayoutScale(roundingHost);
            var left = LayoutHelper.RoundLayoutValue(transformedRect.X, scale);
            var top = LayoutHelper.RoundLayoutValue(transformedRect.Y, scale);
            var right = LayoutHelper.RoundLayoutValue(transformedRect.Right, scale);
            var bottom = LayoutHelper.RoundLayoutValue(transformedRect.Bottom, scale);
            return new Rect(new Point(left, top), new Point(right, bottom));
        }

        /// <summary>
        /// Visuals before the host in composition tree order are clipped out of the hole.
        /// The host itself stays unclipped so its Src-clear can punch the hole.
        /// Later siblings draw over it.
        /// </summary>
        public static bool ClipVisualOutOfHole(int visualTreeOrder, int hostTreeOrder) =>
            visualTreeOrder < hostTreeOrder;

        /// <summary>
        /// Host-local corner radii after intersecting ancestor ClipToBounds rounded clips.
        /// HWND stays rectangular; Skia hole and portal exclude use this path.
        /// </summary>
        public static CornerRadius GetHoleCornerRadius(Layoutable host, Visual root)
        {
            var hostBounds = TransformLocalRectToRoot(host, new Rect(host.Bounds.Size), root, host);
            if (hostBounds == null || hostBounds.Value.IsEmpty())
                return default;

            var result = default(CornerRadius);
            for (Visual visual = host; visual != null; visual = visual.GetVisualParent())
            {
                if (visual is IVisualWithRoundRectClip round)
                {
                    var radius = round.ClipToBoundsRadius;
                    if (radius != default)
                    {
                        var ancestor = visual == host
                            ? hostBounds
                            : TransformLocalRectToRoot(visual, new Rect(visual.Bounds.Size), root, host);
                        if (ancestor != null)
                            result = Max(result, CornerRadiusForHost(hostBounds.Value, ancestor.Value, radius));
                    }
                }

                if (visual == root)
                    break;
            }

            return result;
        }

        public static CornerRadius CornerRadiusForHost(Rect host, Rect ancestor, CornerRadius ancestorRadius)
        {
            var insetLeft = host.X - ancestor.X;
            var insetTop = host.Y - ancestor.Y;
            var insetRight = ancestor.Right - host.Right;
            var insetBottom = ancestor.Bottom - host.Bottom;
            return new CornerRadius(
                Math.Max(0, ancestorRadius.TopLeft - Math.Max(insetLeft, insetTop)),
                Math.Max(0, ancestorRadius.TopRight - Math.Max(insetRight, insetTop)),
                Math.Max(0, ancestorRadius.BottomRight - Math.Max(insetRight, insetBottom)),
                Math.Max(0, ancestorRadius.BottomLeft - Math.Max(insetLeft, insetBottom)));
        }

        public static CornerRadius Max(CornerRadius a, CornerRadius b) =>
            new(
                Math.Max(a.TopLeft, b.TopLeft),
                Math.Max(a.TopRight, b.TopRight),
                Math.Max(a.BottomRight, b.BottomRight),
                Math.Max(a.BottomLeft, b.BottomLeft));

        /// <summary>
        /// Window bounds minus hole AABBs. Used to keep Mica/Acrylic out of native islands.
        /// </summary>
        public static LtrbRect[] SubtractHoles(LtrbRect bounds, IReadOnlyList<LtrbRect> holes)
        {
            if (bounds.IsEmpty)
                return Array.Empty<LtrbRect>();
            if (holes == null || holes.Count == 0)
                return new[] { bounds };

            var current = new List<LtrbRect> { bounds };
            var next = new List<LtrbRect>();
            for (var i = 0; i < holes.Count; i++)
            {
                next.Clear();
                var hole = holes[i];
                for (var j = 0; j < current.Count; j++)
                    SubtractRect(current[j], hole, next);
                (current, next) = (next, current);
            }

            return current.ToArray();
        }

        private static void SubtractRect(LtrbRect a, LtrbRect b, List<LtrbRect> output)
        {
            var clip = a.IntersectOrEmpty(b);
            if (clip.IsEmpty)
            {
                output.Add(a);
                return;
            }

            if (clip.Top > a.Top)
                output.Add(new LtrbRect(a.Left, a.Top, a.Right, clip.Top));
            if (clip.Bottom < a.Bottom)
                output.Add(new LtrbRect(a.Left, clip.Bottom, a.Right, a.Bottom));
            if (clip.Left > a.Left)
                output.Add(new LtrbRect(a.Left, clip.Top, clip.Left, clip.Bottom));
            if (clip.Right < a.Right)
                output.Add(new LtrbRect(clip.Right, clip.Top, a.Right, clip.Bottom));
        }
    }
}
