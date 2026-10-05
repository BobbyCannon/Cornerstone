using System;

namespace Cornerstone.Presentation.PropertyStore
{
    /// <summary>
    /// Raises <see cref="PresentationProperty.Notifying"/> where necessary.
    /// </summary>
    /// <remarks>
    /// Uses the disposable pattern to ensure that the closing Notifying call is made even in the
    /// presence of exceptions. 
    /// </remarks>
    internal struct PropertyNotifying : IDisposable
    {
        private readonly PresentationObject? _owner;
        private Action<PresentationObject, bool>? _notifying;

        private PropertyNotifying(PresentationObject owner, Action<PresentationObject, bool>? notifying)
        {
            _owner = owner;
            _notifying = notifying;
            notifying?.Invoke(owner, true);
        }

        public void Dispose()
        {
            if (_notifying is null)
                return;

            _notifying(_owner!, false);
            _notifying = null;
        }

        public static PropertyNotifying Start(PresentationObject owner, PresentationProperty property)
            => new(owner, property.Notifying);
    }
}
