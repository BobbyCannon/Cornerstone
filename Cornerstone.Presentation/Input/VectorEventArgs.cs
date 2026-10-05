using System;
using Cornerstone.Presentation.Interactivity;

namespace Cornerstone.Presentation.Input
{
    public class VectorEventArgs : RoutedEventArgs
    {
        public Vector Vector { get; init; }
    }
}
