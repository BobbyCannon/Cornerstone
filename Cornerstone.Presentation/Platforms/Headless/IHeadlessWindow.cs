using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Raw;
using Cornerstone.Presentation.Media.Imaging;

namespace Cornerstone.Presentation.Headless
{
    internal interface IHeadlessWindow
    {
        WriteableBitmap? GetLastRenderedFrame();
        void KeyPress(Key key, RawInputModifiers modifiers, PhysicalKey physicalKey, string? keySymbol);
        void KeyRelease(Key key, RawInputModifiers modifiers, PhysicalKey physicalKey, string? keySymbol);
        void TextInput(string text);
        void MouseDown(Point point, MouseButton button, RawInputModifiers modifiers = RawInputModifiers.None);
        void MouseMove(Point point, RawInputModifiers modifiers = RawInputModifiers.None);
        void MouseUp(Point point, MouseButton button, RawInputModifiers modifiers = RawInputModifiers.None);
        void MouseWheel(Point point, Vector delta, RawInputModifiers modifiers = RawInputModifiers.None);
        void Touch(Point point, long touchPointId, RawPointerEventType type, RawInputModifiers modifiers = RawInputModifiers.None);
        void DragDrop(Point point, RawDragEventType type, IDataTransfer data, DragDropEffects effects, RawInputModifiers modifiers = RawInputModifiers.None);
        void SetRenderScaling(double scaling);
    }
}
