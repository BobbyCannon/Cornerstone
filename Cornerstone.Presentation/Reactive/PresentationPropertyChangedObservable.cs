using System;

namespace Cornerstone.Presentation.Reactive
{
    internal class PresentationPropertyChangedObservable : 
        LightweightObservableBase<PresentationPropertyChangedEventArgs>,
        IDescription
    {
        private readonly WeakReference<PresentationObject> _target;
        private readonly PresentationProperty _property;

        public PresentationPropertyChangedObservable(
            PresentationObject target,
            PresentationProperty property)
        {
            _target = new WeakReference<PresentationObject>(target);
            _property = property;
        }

        public string Description => $"{_target.GetType().Name}.{_property.Name}";

        protected override void Initialize()
        {
            if (_target.TryGetTarget(out var target))
            {
                target.PropertyChanged += PropertyChanged;
            }
        }

        protected override void Deinitialize()
        {
            if (_target.TryGetTarget(out var target))
            {
                target.PropertyChanged -= PropertyChanged;
            }
        }

        private void PropertyChanged(object? sender, PresentationPropertyChangedEventArgs e)
        {
            if (e.Property == _property)
            {
                PublishNext(e);
            }
        }
    }
}
