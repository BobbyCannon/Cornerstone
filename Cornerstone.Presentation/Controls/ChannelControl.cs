#region References

using System;
using Cornerstone.Communications;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Drawing;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Media;
using Cornerstone.Profiling;
using Cornerstone.Presentation.Controls.DesignTime;

#endregion

namespace Cornerstone.Presentation.Controls;

public partial class ChannelControl : TemplatedControl, IDispatchable
{
    #region Constants

    private const double PreferredCellSize = 16;

    #endregion

    #region Fields

    private Pen _borderPen;
    private readonly DrawingContextHelper _contextHelper;
    private readonly Throttle _invalidateThrottle;
    private LayoutPlan _layoutPlan;

    #endregion

    #region Constructors

    public ChannelControl()
    {
        _contextHelper = new DrawingContextHelper(this);
        _invalidateThrottle = new Throttle(InvalidateFromData, TimeSpan.FromMilliseconds(1000 / 60));
        _layoutPlan = new LayoutPlan();

        if (Design.IsDesignMode)
        {
            var buffer = new byte[128];
            var server = new MemoryChannel(buffer);
            var client = new MemoryChannel(server);
            client.Send([1, 2, 3, 4, 5, 6], 0, 6);
            Channel = server;
        }
    }

    static ChannelControl()
    {
        AffectsRender<ChannelControl>(
            ForegroundProperty,
            ShowLabelsProperty,
            StrokeProperty
        );

        AffectsMeasure<ChannelControl>(
            FontFamilyProperty,
            FontSizeProperty,
            FontStyleProperty,
            FontWeightProperty,
            ShowLabelsProperty
        );
    }

    #endregion

    #region Properties

    [StyledProperty(DefaultValue = 16)]
    public partial int BytesPerGroup { get; set; }

    [StyledProperty]
    public partial Channel Channel { get; set; }

    [StyledProperty]
    public partial Profiler Profiler { get; set; }

    [StyledProperty(DefaultValue = true)]
    public partial bool ShowLabels { get; set; }

    [StyledProperty]
    public partial IBrush Stroke { get; set; }

    [StyledProperty]
    public partial string Title { get; set; }

    #endregion

    #region Methods

    public IDispatcher GetDispatcher()
    {
        return Dispatcher;
    }

    public override void Render(DrawingContext context)
    {
        using var start = ProfilerExtensions.Start(Profiler, "Render");

        var borderThickness = LayoutHelpers.GetBestSingle(BorderThickness);
        var cornerRadius = (float) LayoutHelpers.GetBestSingle(CornerRadius);
        var backgroundArea = new Rect(Bounds.Size);

        // Draw background + border
        if ((BorderBrush != null) && (borderThickness > 0))
        {
            backgroundArea = backgroundArea.Deflate(borderThickness * 0.5);
            var roundedRect = new RoundedRect(backgroundArea, CornerRadius.TopLeft, CornerRadius.TopRight,
                CornerRadius.BottomRight, CornerRadius.BottomLeft);
            context.DrawRectangle(Background, _borderPen ??= new Pen(BorderBrush), roundedRect);
            backgroundArea = backgroundArea.Deflate(borderThickness * 0.5);
        }
        else
        {
            context.DrawRectangle(Background, null, backgroundArea);
        }

        using var clip = context.PushClip(new RoundedRect(backgroundArea, cornerRadius));

        var state = Channel?.GetQueueState();
        if (state != null)
        {
            DrawQueue(context, backgroundArea, state, Channel.IsServer ? "Server" : "Client");
        }

        base.Render(context);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        using var _ = ProfilerExtensions.Start(Profiler, "Measure");

        var state = Channel?.GetQueueState();
        if (state is null || (state.Capacity <= 0))
        {
            _layoutPlan.Invalidate();
            return new Size(200, 100);
        }

        var border = BorderThickness;
        var borderWidth = border.Left + border.Right;
        var borderHeight = border.Top + border.Bottom;

        var padding = Padding;
        var paddingWidth = padding.Left + padding.Right;
        var paddingHeight = padding.Top + padding.Bottom;

        var cellSize = PreferredCellSize;
        var usableWidth = availableSize.Width - paddingWidth - borderWidth;
        var byteSize = BytesPerGroup * cellSize;

        var columns = double.IsPositiveInfinity(availableSize.Width)
            ? (int) (32 * cellSize)
            : (int) Math.Floor(usableWidth / byteSize) * BytesPerGroup;
        columns = Math.Max(columns, BytesPerGroup);

        var rows = ((state.Capacity + columns) - 1) / columns;
        var gridWidth = columns * cellSize;
        var gridHeight = rows * cellSize;

        var labelHeight = ShowLabels ? _contextHelper.SpriteHeight + padding.Top : 0.0;

        var desiredWidth = gridWidth + borderWidth + paddingWidth;
        var desiredHeight = gridHeight + labelHeight + borderHeight + paddingHeight;

        desiredWidth = Math.Max(desiredWidth, 200);
        desiredHeight = Math.Max(desiredHeight, 60);

        var finalWidth = double.IsPositiveInfinity(availableSize.Width)
            ? desiredWidth
            : Math.Min(desiredWidth, availableSize.Width);

        var finalHeight = double.IsPositiveInfinity(availableSize.Height)
            ? desiredHeight
            : Math.Min(desiredHeight, availableSize.Height);

        // Cache the layout plan
        _layoutPlan.Columns = columns;
        _layoutPlan.CellSize = cellSize;
        _layoutPlan.LabelHeight = labelHeight;
        _layoutPlan.ShowLabels = ShowLabels;
        _layoutPlan.GridOffsetX = border.Left + padding.Left;
        _layoutPlan.GridOffsetY = ShowLabels
            ? border.Top + padding.Top + labelHeight
            : border.Top + padding.Top;

        return new Size(finalWidth, finalHeight);
    }

    protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ChannelProperty)
        {
            if (change.OldValue is Channel oldChannel)
            {
                oldChannel.DataChanged -= OnDataChanged;
                _invalidateThrottle.Cancel();
            }
            if (change.NewValue is Channel newChannel)
            {
                newChannel.DataChanged += OnDataChanged;
                _invalidateThrottle.Trigger(true);
            }
        }
    }

    private void DrawQueue(DrawingContext context, Rect clientArea, Channel.QueueState state, string title)
    {
        if (!_layoutPlan.IsValid || (state.Capacity == 0))
        {
            var visualX = clientArea.Center.X - 80;
            var visualY = clientArea.Center.Y - 6;
            _contextHelper.Draw(context, $"{title} — not initialized", ref visualX, ref visualY);
            return;
        }

        var plan = _layoutPlan;
        var offsetX = clientArea.X + plan.GridOffsetX;
        var offsetY = clientArea.Y + plan.GridOffsetY;

        var usedBrush = Stroke ?? Brushes.SteelBlue;
        var emptyBrush = Background?.ToImmutable() ?? Brushes.DarkSlateGray;

        var read = state.ReadPos;
        var write = state.WritePos;
        double capacity = state.Capacity;
        var used = ((write - read) + capacity) % capacity;

        for (var i = 0; i < capacity; i++)
        {
            var col = i % plan.Columns;
            var row = i / plan.Columns;

            var rect = new Rect(
                offsetX + (col * plan.CellSize),
                offsetY + (row * plan.CellSize),
                plan.CellSize,
                plan.CellSize);

            var distanceFromRead = ((i - read) + capacity) % capacity;
            var isUsed = distanceFromRead < used;
            var brush = isUsed ? usedBrush : emptyBrush;

            if (i == read)
            {
                brush = Brushes.LimeGreen;
            }
            else if (i == write)
            {
                brush = Brushes.OrangeRed;
            }

            context.FillRectangle(brush, rect);
            context.DrawRectangle(null, _borderPen ??= new Pen(BorderBrush), rect);
        }

        if (plan.ShowLabels)
        {
            var percent = capacity > 0 ? (used * 100.0) / capacity : 0;
            var labelX = _layoutPlan.GridOffsetX;
            var labelY = _layoutPlan.GridOffsetY - _layoutPlan.LabelHeight;
            _contextHelper.Draw(context, title, ref labelX, ref labelY);
            _contextHelper.Draw(context, " • ", ref labelX, ref labelY);
            _contextHelper.Draw(context, used, ref labelX, ref labelY);
            _contextHelper.Draw(context, '/', ref labelX, ref labelY);
            _contextHelper.Draw(context, capacity, ref labelX, ref labelY);
            _contextHelper.Draw(context, " bytes (", ref labelX, ref labelY);
            _contextHelper.Draw(context, percent, ref labelX, ref labelY);
            _contextHelper.Draw(context, " %)", ref labelX, ref labelY);
        }
    }

    private void InvalidateFromData()
    {
        this.Dispatch(InvalidateVisual);
    }

    private void OnDataChanged(object sender, EventArgs e)
    {
        _invalidateThrottle.Trigger();
    }

    #endregion

    #region Structures

    private struct LayoutPlan
    {
        #region Fields

        public double CellSize;
        public int Columns;
        public double GridOffsetX;
        public double GridOffsetY;
        public double LabelHeight;
        public bool ShowLabels;

        #endregion

        #region Properties

        public bool IsValid => CellSize > 0;

        #endregion

        #region Methods

        public void Invalidate()
        {
            this = default;
        }

        #endregion
    }

    #endregion
}