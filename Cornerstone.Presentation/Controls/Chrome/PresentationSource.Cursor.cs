using System;
using Cornerstone.Presentation.Input;

namespace Cornerstone.Presentation.Controls.Chrome;

internal partial class PresentationSource
{
    private Cursor? _cursor;
    private Cursor? _cursorOverride;

    private void UpdateCursor() => PlatformImpl?.SetCursor(_cursorOverride?.PlatformImpl ?? _cursor?.PlatformImpl);

    private void SetCursor(Cursor? cursor)
    {
        _cursor = cursor;
        UpdateCursor();
    }

    /// <summary>
    /// This should only be used by InProcessDragSource
    /// </summary>
    internal void SetCursorOverride(Cursor? cursor)
    {
        _cursorOverride = cursor;
        UpdateCursor();
    }

    IInputElement? IInputRoot.PointerOverElement { get; set; }

    IInputElement? IInputRoot.CursorElement
    {
        get;
        set
        {
            if (field == value)
                return;

            if (field is PresentationObject old)
                old.PropertyChanged -= CursorElement_PropertyChanged;
            field = value;
            if (field is PresentationObject @new)
                @new.PropertyChanged += CursorElement_PropertyChanged;
            SetCursor(value?.Cursor);
        }
    }

    void IInputRoot.PointerOverInvalidated()
        => _pointerOverPreProcessor?.SceneInvalidated(new Rect(new Point(0, 0), Size.Infinity));

    private void CursorElement_PropertyChanged(object? sender, PresentationPropertyChangedEventArgs e)
    {
        if (e.Property == InputElement.CursorProperty)
            SetCursor((sender as IInputElement)?.Cursor);
    }
}
