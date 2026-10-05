using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Input.TextInput
{
    [Unstable]
    public interface ITextInputMethodImpl
    {
        void SetClient(TextInputMethodClient? client);
        void SetCursorRect(Rect rect);
        void SetOptions(TextInputOptions options);
        void Reset();
    }
}
