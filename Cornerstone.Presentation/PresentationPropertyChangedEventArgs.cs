using System;
using Cornerstone.Presentation.Data;

namespace Cornerstone.Presentation
{
    /// <summary>
    /// Provides information for a cornerstone property change.
    /// </summary>
    public abstract class PresentationPropertyChangedEventArgs : EventArgs
    {
        public PresentationPropertyChangedEventArgs(
            PresentationObject sender,
            BindingPriority priority)
        {
            Sender = sender;
            Priority = priority;
            IsEffectiveValueChange = true;
        }

        internal PresentationPropertyChangedEventArgs(
            PresentationObject sender,
            BindingPriority priority,
            bool isEffectiveValueChange)
        {
            Sender = sender;
            Priority = priority;
            IsEffectiveValueChange = isEffectiveValueChange;
        }

        /// <summary>
        /// Gets the <see cref="PresentationObject"/> that the property changed on.
        /// </summary>
        /// <value>The sender object.</value>
        public PresentationObject Sender { get; private set; }

        /// <summary>
        /// Gets the property that changed.
        /// </summary>
        /// <value>
        /// The property that changed.
        /// </value>
        public PresentationProperty Property => GetProperty();

        /// <summary>
        /// Gets the old value of the property.
        /// </summary>
        public object? OldValue => GetOldValue();

        /// <summary>
        /// Gets the new value of the property.
        /// </summary>
        public object? NewValue => GetNewValue();

        /// <summary>
        /// Gets the priority of the binding that produced the value.
        /// </summary>
        /// <value>
        /// The priority of the new value.
        /// </value>
        public BindingPriority Priority { get; private set; }

        internal bool IsEffectiveValueChange { get; private set; }
        
        /// <summary>
        /// Sets the Sender property.
        /// This is purely for reuse in some code paths where multiple allocations may occur.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        internal void SetSender(PresentationObject sender)
        {
            Sender = sender;
        }

        protected abstract PresentationProperty GetProperty();
        protected abstract object? GetOldValue();
        protected abstract object? GetNewValue();
    }
}
