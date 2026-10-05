using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.VisualTree;

namespace Cornerstone.Presentation.Platform
{
    /// <summary>
    /// Windows layered windows deliver hits from the composition bitmap alpha.
    /// Alpha 0 never reaches WM_NCHITTEST, so a hit-testable Cornerstone visual
    /// that paints nothing would lose the event to the native child.
    /// Coverage that hit-tests as Cornerstone keeps an alpha of 1. Uncovered hole
    /// pixels stay 0. See Presentation/NativeLayering.md.
    /// </summary>
    internal static class NativeAirspaceHitAlpha
    {
        public const byte MinimumInteractiveAlpha = 1;
        public const int MaxSamples = 4096;

        internal readonly struct Cell
        {
            public readonly int Left;
            public readonly int Top;
            public readonly int Right;
            public readonly int Bottom;

            public Cell(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }
        }

        private readonly struct PixelRect
        {
            public readonly int Left;
            public readonly int Top;
            public readonly int Right;
            public readonly int Bottom;

            public PixelRect(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }
        }

        public static void CollectSplitters(Visual root, Rect holeDip, double scaling, List<LtrbRect> into)
        {
            if (root == null || into == null || holeDip.Width <= 0 || holeDip.Height <= 0 || scaling <= 0)
                return;
            Collect(root, holeDip, scaling, into);
        }

        public static void BuildInteractiveCells(
            LtrbRect holePixels,
            IReadOnlyList<LtrbRect> splitters,
            List<Cell> cells,
            Func<double, double, bool> isInteractiveAtPixel)
        {
            if (cells == null)
                throw new ArgumentNullException(nameof(cells));
            if (isInteractiveAtPixel == null)
                throw new ArgumentNullException(nameof(isInteractiveAtPixel));

            var left = FloorPixel(holePixels.Left);
            var top = FloorPixel(holePixels.Top);
            var right = CeilPixel(holePixels.Right);
            var bottom = CeilPixel(holePixels.Bottom);
            if (right <= left || bottom <= top)
                return;

            var builder = new CoverageBuilder(ToPixelRects(splitters, left, top, right, bottom), cells, isInteractiveAtPixel);
            builder.Visit(left, top, right, bottom, 0);
        }

        /// <summary>
        /// Drops the previous alpha-1 sentinel, then raises alpha on the new coverage.
        /// The sentinel is premultiplied black at alpha 1. Real painted alpha is left alone.
        /// </summary>
        public static void Apply(
            Span<byte> bgra,
            int width,
            int height,
            int rowBytes,
            IReadOnlyList<Cell> previous,
            IReadOnlyList<Cell> next)
        {
            if (bgra.IsEmpty || width <= 0 || height <= 0 || rowBytes < width * 4)
                return;
            var needed = (long)rowBytes * height;
            if (needed > bgra.Length)
                return;

            RevertSentinel(bgra, width, height, rowBytes, previous);
            RaiseAlpha(bgra, width, height, rowBytes, next);
        }

        private static void Collect(Visual visual, Rect holeDip, double scaling, List<LtrbRect> into)
        {
            if (!visual.IsVisible || visual is Controls.NativeControlHost)
                return;

            var transformed = visual.GetTransformedBounds();
            if (transformed == null)
                return;

            var bounds = transformed.Value;
            var world = bounds.Bounds.TransformToAABB(bounds.Transform);
            if (!bounds.Clip.IsEmpty())
                world = world.Intersect(bounds.Clip);
            world = world.Intersect(holeDip);
            var missedHole = world.Width <= 0 || world.Height <= 0;
            if (!missedHole && visual is IInputElement { IsHitTestVisible: true })
            {
                into.Add(new LtrbRect(
                    world.X * scaling,
                    world.Y * scaling,
                    world.Right * scaling,
                    world.Bottom * scaling));
            }

            if (missedHole && visual.ClipToBounds)
                return;

            foreach (var child in visual.GetVisualChildren())
                Collect(child, holeDip, scaling, into);
        }

        private static PixelRect[] ToPixelRects(IReadOnlyList<LtrbRect> splitters, int holeLeft, int holeTop, int holeRight, int holeBottom)
        {
            if (splitters == null || splitters.Count == 0)
                return Array.Empty<PixelRect>();

            var rects = new List<PixelRect>(splitters.Count);
            for (var i = 0; i < splitters.Count; i++)
            {
                var splitter = splitters[i];
                var left = FloorPixel(splitter.Left);
                var top = FloorPixel(splitter.Top);
                var right = CeilPixel(splitter.Right);
                var bottom = CeilPixel(splitter.Bottom);
                if (right <= left || bottom <= top)
                    continue;
                if (right <= holeLeft || left >= holeRight || bottom <= holeTop || top >= holeBottom)
                    continue;
                if (left <= holeLeft && top <= holeTop && right >= holeRight && bottom >= holeBottom)
                    continue;
                rects.Add(new PixelRect(left, top, right, bottom));
            }

            return rects.ToArray();
        }

        private static int FloorPixel(double value)
        {
            if (value <= 0 || double.IsNaN(value))
                return 0;
            if (value >= 32767)
                return 32767;
            return (int)Math.Floor(value);
        }

        private static int CeilPixel(double value)
        {
            if (value <= 0 || double.IsNaN(value))
                return 0;
            if (value >= 32768)
                return 32768;
            return (int)Math.Ceiling(value);
        }

        private static void RevertSentinel(Span<byte> bgra, int width, int height, int rowBytes, IReadOnlyList<Cell> cells)
        {
            if (cells == null)
                return;
            for (var i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                var left = Clamp(cell.Left, 0, width);
                var top = Clamp(cell.Top, 0, height);
                var right = Clamp(cell.Right, 0, width);
                var bottom = Clamp(cell.Bottom, 0, height);
                for (var y = top; y < bottom; y++)
                {
                    var row = y * rowBytes;
                    for (var x = left; x < right; x++)
                    {
                        var pixel = row + (x * 4);
                        if (bgra[pixel] == 0 && bgra[pixel + 1] == 0 && bgra[pixel + 2] == 0
                            && bgra[pixel + 3] == MinimumInteractiveAlpha)
                            bgra[pixel + 3] = 0;
                    }
                }
            }
        }

        private static void RaiseAlpha(Span<byte> bgra, int width, int height, int rowBytes, IReadOnlyList<Cell> cells)
        {
            if (cells == null)
                return;
            for (var i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                var left = Clamp(cell.Left, 0, width);
                var top = Clamp(cell.Top, 0, height);
                var right = Clamp(cell.Right, 0, width);
                var bottom = Clamp(cell.Bottom, 0, height);
                for (var y = top; y < bottom; y++)
                {
                    var row = y * rowBytes;
                    for (var x = left; x < right; x++)
                    {
                        var alpha = row + (x * 4) + 3;
                        if (bgra[alpha] == 0)
                            bgra[alpha] = MinimumInteractiveAlpha;
                    }
                }
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        private sealed class CoverageBuilder
        {
            private readonly PixelRect[] _splitters;
            private readonly List<Cell> _cells;
            private readonly Func<double, double, bool> _isInteractive;
            private int _samples;

            public CoverageBuilder(PixelRect[] splitters, List<Cell> cells, Func<double, double, bool> isInteractive)
            {
                _splitters = splitters;
                _cells = cells;
                _isInteractive = isInteractive;
            }

            public void Visit(int left, int top, int right, int bottom, int depth)
            {
                if (left >= right || top >= bottom)
                    return;

                var centerX = left + ((right - left) * 0.5);
                var centerY = top + ((bottom - top) * 0.5);
                if ((right - left <= 1 && bottom - top <= 1) || depth >= 24 || _samples >= MaxSamples)
                {
                    if (Hit(centerX, centerY))
                        _cells.Add(new Cell(left, top, right, bottom));
                    return;
                }

                var agreement = SamplesAgree(left, top, right, bottom);
                if (agreement != null && !HasPartialSplitter(left, top, right, bottom))
                {
                    if (agreement.Value)
                        _cells.Add(new Cell(left, top, right, bottom));
                    return;
                }

                var midX = left + ((right - left) / 2);
                var midY = top + ((bottom - top) / 2);
                if (midX <= left && midY <= top)
                {
                    if (Hit(centerX, centerY))
                        _cells.Add(new Cell(left, top, right, bottom));
                    return;
                }

                if (midX > left && midY > top)
                {
                    Visit(left, top, midX, midY, depth + 1);
                    Visit(midX, top, right, midY, depth + 1);
                    Visit(left, midY, midX, bottom, depth + 1);
                    Visit(midX, midY, right, bottom, depth + 1);
                    return;
                }

                if (midX > left)
                {
                    Visit(left, top, midX, bottom, depth + 1);
                    Visit(midX, top, right, bottom, depth + 1);
                    return;
                }

                Visit(left, top, right, midY, depth + 1);
                Visit(left, midY, right, bottom, depth + 1);
            }

            private bool? SamplesAgree(int left, int top, int right, int bottom)
            {
                var x0 = left + 0.5;
                var x1 = right - 0.5;
                var y0 = top + 0.5;
                var y1 = bottom - 0.5;
                var first = Hit(x0, y0);
                if (x1 != x0 && Hit(x1, y0) != first)
                    return null;
                if (y1 != y0 && Hit(x0, y1) != first)
                    return null;
                if (x1 != x0 && y1 != y0 && Hit(x1, y1) != first)
                    return null;

                var centerX = left + ((right - left) * 0.5);
                var centerY = top + ((bottom - top) * 0.5);
                var centerIsCorner = (centerX == x0 && centerY == y0)
                    || (centerX == x1 && centerY == y0)
                    || (centerX == x0 && centerY == y1)
                    || (centerX == x1 && centerY == y1);
                if (!centerIsCorner && Hit(centerX, centerY) != first)
                    return null;
                return first;
            }

            private bool HasPartialSplitter(int left, int top, int right, int bottom)
            {
                for (var i = 0; i < _splitters.Length; i++)
                {
                    var splitter = _splitters[i];
                    if (splitter.Right <= left || splitter.Left >= right || splitter.Bottom <= top || splitter.Top >= bottom)
                        continue;
                    if (splitter.Left <= left && splitter.Top <= top && splitter.Right >= right && splitter.Bottom >= bottom)
                        continue;
                    return true;
                }

                return false;
            }

            private bool Hit(double x, double y)
            {
                _samples++;
                return _isInteractive(x, y);
            }
        }
    }
}
