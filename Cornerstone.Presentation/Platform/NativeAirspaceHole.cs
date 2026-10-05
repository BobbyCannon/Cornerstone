namespace Cornerstone.Presentation.Platform
{
    /// <summary>
    /// A native island hole in root coordinates. HWND stays AABB; Skia/portal use CornerRadius.
    /// </summary>
    internal interface INativeAirspaceHole
    {
        Rect Bounds { get; }
        LtrbRect PhysicalBounds { get; }
        CornerRadius CornerRadius { get; }
        int TreeOrder { get; }
        int NativeZ { get; }
    }

    internal readonly struct NativeAirspaceHole : INativeAirspaceHole
    {
        public NativeAirspaceHole(Rect bounds, double scaling, int treeOrder, int nativeZ, object hostServer,
            CornerRadius cornerRadius = default)
        {
            Bounds = bounds;
            TreeOrder = treeOrder;
            NativeZ = nativeZ;
            HostServer = hostServer;
            CornerRadius = cornerRadius;
            Scaling = scaling;
            PhysicalBounds = bounds.IsEmpty()
                ? default
                : new LtrbRect(
                    bounds.X * scaling,
                    bounds.Y * scaling,
                    bounds.Right * scaling,
                    bounds.Bottom * scaling);
        }

        public Rect Bounds { get; }
        public LtrbRect PhysicalBounds { get; }
        public CornerRadius CornerRadius { get; }
        public double Scaling { get; }
        public int TreeOrder { get; }
        public int NativeZ { get; }
        public object HostServer { get; }

        public bool IsEmpty => Bounds.IsEmpty();

        public bool IsRounded =>
            CornerRadius.TopLeft > 0 || CornerRadius.TopRight > 0
            || CornerRadius.BottomRight > 0 || CornerRadius.BottomLeft > 0;

        public float PhysicalRadiusTopLeft => (float)(CornerRadius.TopLeft * Scaling);
        public float PhysicalRadiusTopRight => (float)(CornerRadius.TopRight * Scaling);
        public float PhysicalRadiusBottomRight => (float)(CornerRadius.BottomRight * Scaling);
        public float PhysicalRadiusBottomLeft => (float)(CornerRadius.BottomLeft * Scaling);
    }
}
