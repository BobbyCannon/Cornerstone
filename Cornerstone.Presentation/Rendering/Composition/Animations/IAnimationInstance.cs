using System;
using Cornerstone.Presentation.Rendering.Composition.Expressions;
using Cornerstone.Presentation.Rendering.Composition.Server;

namespace Cornerstone.Presentation.Rendering.Composition.Animations
{
    internal interface IAnimationInstance : IServerClockItem
    {
        ServerObject TargetObject { get; }
        ExpressionVariant Evaluate(TimeSpan now, ExpressionVariant currentValue);
        void Initialize(TimeSpan startedAt, ExpressionVariant startingValue, CompositionProperty property);
        void Activate();
        void Deactivate();
        void Invalidate();
    }
}
