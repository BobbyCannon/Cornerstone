using System;
using Cornerstone.Presentation.Animation.Animators;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Transformation;

#nullable enable

namespace Cornerstone.Presentation.Animation
{
    public class TransformOperationsTransition : Transition<ITransform>
    {
        private static readonly TransformOperationsAnimator s_operationsAnimator = new TransformOperationsAnimator();

        internal override IObservable<ITransform> DoTransition(
            IObservable<double> progress,
            ITransform oldValue,
            ITransform newValue)
        {
            var oldTransform = TransformOperationsAnimator.EnsureOperations(oldValue);
            var newTransform = TransformOperationsAnimator.EnsureOperations(newValue);

            return new AnimatorTransitionObservable<TransformOperations, TransformOperationsAnimator>(
                s_operationsAnimator, progress, Easing, oldTransform, newTransform);
        }
    }
}
