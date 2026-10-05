using System;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Platform;

namespace Cornerstone.Presentation.Android.Platform
{
    internal class AndroidSystemNavigationManagerImpl : ISystemNavigationManagerImpl, IDisposable
    {
        private readonly IActivityNavigationService? _navigationService;

        public event EventHandler<RoutedEventArgs>? BackRequested;

        public AndroidSystemNavigationManagerImpl(IActivityNavigationService? navigationService)
        {
            if(navigationService != null)
            {
                navigationService.BackRequested += OnBackRequested;
            }
            _navigationService = navigationService;
        }

        private void OnBackRequested(object? sender, AndroidBackRequestedEventArgs e)
        {
            var routedEventArgs = new RoutedEventArgs();

            BackRequested?.Invoke(this, routedEventArgs);

            e.Handled = routedEventArgs.Handled;
        }

        public void Dispose()
        {
            if (_navigationService != null)
            {
                _navigationService.BackRequested -= OnBackRequested;
            }
        }
    }
}
