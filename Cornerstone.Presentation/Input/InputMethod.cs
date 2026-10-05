using System;
using Cornerstone.Presentation.Input.TextInput;
using Cornerstone.Presentation.Interactivity;

namespace Cornerstone.Presentation.Input
{
    public class InputMethod
    {
        /// <summary>
        ///     A dependency property that enables alternative text inputs.
        /// </summary>
        public static readonly PresentationProperty<bool> IsInputMethodEnabledProperty =
            PresentationProperty.RegisterAttached<InputMethod, InputElement, bool>("IsInputMethodEnabled", true);
        
        /// <summary>
        /// Setter for IsInputMethodEnabled PresentationProperty
        /// </summary>
        public static void SetIsInputMethodEnabled(InputElement target, bool value)
        {
            target.SetValue(IsInputMethodEnabledProperty, value);
        }

        /// <summary>
        /// Getter for IsInputMethodEnabled PresentationProperty
        /// </summary>
        public static bool GetIsInputMethodEnabled(InputElement target)
        {
            return target.GetValue<bool>(IsInputMethodEnabledProperty);
        }
        
        /// <summary>
        /// Defines the TextInputMethodClientRequeryRequested event.
        /// </summary>
        public static readonly RoutedEvent<TextInputMethodClientRequeryRequestedEventArgs> TextInputMethodClientRequeryRequestedEvent =
            RoutedEvent.Register<InputElement, TextInputMethodClientRequeryRequestedEventArgs>(
                "TextInputMethodClientRequeryRequested",
                RoutingStrategies.Bubble);
        
        public static void AddTextInputMethodClientRequeryRequestedHandler(Interactive element, EventHandler<RoutedEventArgs> handler)
        {
            element.AddHandler(TextInputMethodClientRequeryRequestedEvent, handler);
        }
        
        public static void RemoveTextInputMethodClientRequeryRequestedHandler(Interactive element, EventHandler<RoutedEventArgs> handler)
        {
            element.RemoveHandler(TextInputMethodClientRequeryRequestedEvent, handler);
        }
        
        private InputMethod()
        {

        }
    }
}
