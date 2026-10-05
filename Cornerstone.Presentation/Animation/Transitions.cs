using System;
using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Threading;

namespace Cornerstone.Presentation.Animation
{
    /// <summary>
    /// A collection of <see cref="ITransition"/> definitions.
    /// </summary>
    public sealed class Transitions : OldPresentationList<ITransition>, IOldPresentationListItemValidator<ITransition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Transitions"/> class.
        /// </summary>
        public Transitions()
        {
            ResetBehavior = ResetBehavior.Remove;
            Validator = this;
        }

        void IOldPresentationListItemValidator<ITransition>.Validate(ITransition item)
        {
            Dispatcher.UIThread.VerifyAccess();

            var property = item.Property;
            if (property.IsDirect)
            {
                var display = item is TransitionBase transition ? transition.DebugDisplay : item.ToString();
                throw new InvalidOperationException($"Cannot animate direct property {property} on {display}.");
            }
        }
    }
}
