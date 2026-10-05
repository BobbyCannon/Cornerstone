using Cornerstone.Presentation.Interactivity;

namespace Cornerstone.Presentation.Input.TextInput
{
    public class TextInputMethodClientRequestedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Set this property to a valid text input client to enable input method interaction
        /// </summary>
        public TextInputMethodClient? Client { get; set; }
    }
}
