using Cornerstone.Presentation.Input.TextInput;
using UIKit;

namespace Cornerstone.Presentation.iOS;

public partial class CornerstoneView
{
    private const string ImeLog = "IOSIME";
    private Rect _cursorRect;
    private TextInputOptions? _options;

    private static UIResponder? CurrentCornerstoneResponder { get; set; }
    public override bool BecomeFirstResponder()
    {
        var res = base.BecomeFirstResponder();
        if (res)
            CurrentCornerstoneResponder = this;
        return res;
    }

    public override bool ResignFirstResponder()
    {
        var res = base.ResignFirstResponder();
        if (res && ReferenceEquals(CurrentCornerstoneResponder, this))
            CurrentCornerstoneResponder = null;
        return res;
    }

    private bool IsDrivingText => CurrentCornerstoneResponder is TextInputResponder t && ReferenceEquals(t.NextResponder, this);

    void ITextInputMethodImpl.SetClient(TextInputMethodClient? client)
    {
        _client = client;
        if (_client == null && IsDrivingText)
            BecomeFirstResponder();            

        if (_client is { })
        {
            new TextInputResponder(this, _client).BecomeFirstResponder();
        }
    }

    void ITextInputMethodImpl.SetCursorRect(Rect rect) => _cursorRect = rect;

    void ITextInputMethodImpl.SetOptions(TextInputOptions options) => _options = options;

    void ITextInputMethodImpl.Reset()
    {
        if (IsDrivingText)
            BecomeFirstResponder();
    }
}
