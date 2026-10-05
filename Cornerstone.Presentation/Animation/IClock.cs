using System;
using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Animation
{
    internal interface IClock : IObservable<TimeSpan>
    {
        PlayState PlayState { get; set; }
    }
}
