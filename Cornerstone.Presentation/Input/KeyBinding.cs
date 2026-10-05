using System.Windows.Input;

namespace Cornerstone.Presentation.Input
{
    public class KeyBinding : PresentationObject
    {
        public static readonly StyledProperty<ICommand> CommandProperty =
            PresentationProperty.Register<KeyBinding, ICommand>(nameof(Command));

        public ICommand Command
        {
            get { return GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public static readonly StyledProperty<object> CommandParameterProperty =
            PresentationProperty.Register<KeyBinding, object>(nameof(CommandParameter));

        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        public static readonly StyledProperty<KeyGesture> GestureProperty =
            PresentationProperty.Register<KeyBinding, KeyGesture>(nameof(Gesture));

        public KeyGesture Gesture
        {
            get { return GetValue(GestureProperty); }
            set { SetValue(GestureProperty, value); }
        }

        public void TryHandle(KeyEventArgs args)
        {
            if (Gesture?.Matches(args) == true)
            {
                if (Command?.CanExecute(CommandParameter) == true) 
                {
                    args.Handled = true;
                    Command.Execute(CommandParameter);
                }
            }
        }
    }
}
