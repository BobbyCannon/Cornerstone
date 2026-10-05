using System;

namespace Cornerstone.Presentation.Input
{
    public interface ICloseable
    {
        event EventHandler? Closed;
    }
}
