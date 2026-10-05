namespace Cornerstone.Presentation.SourceGenerator.CompositionGenerator
{
    public partial class Generator
    {
        void GenerateAnimations()
        {
            var code = $@"using System.Numerics;
using Cornerstone.Presentation.Rendering.Composition.Animations;
using Cornerstone.Presentation.Rendering.Composition.Expressions;

// Special license applies <see href=""https://raw.githubusercontent.com/CornerstoneUI/Cornerstone/master/src/Cornerstone.Presentation.Base/Rendering/Composition/License.md\"">License.md</see>

namespace Cornerstone.Presentation.Rendering.Composition
{{
";

            foreach (var a in _config.KeyFrameAnimations)
            {
                var name = a.Name ?? a.Type;

                code += $@"
    public class {name}KeyFrameAnimation : KeyFrameAnimation
    {{
        public {name}KeyFrameAnimation(Compositor compositor) : base(compositor)
        {{
        }}

        internal override IAnimationInstance CreateInstance(Cornerstone.Presentation.Rendering.Composition.Server.ServerObject targetObject, ExpressionVariant? finalValue)
        {{
            return new KeyFrameAnimationInstance<{a.Type}>({name}Interpolator.Instance, _keyFrames.Snapshot(), CreateSnapshot(), 
                finalValue?.CastOrDefault<{a.Type}>(), targetObject,
                DelayBehavior, DelayTime, Direction, Duration, IterationBehavior,
                IterationCount, StopBehavior);
        }}
        
        private KeyFrames<{a.Type}> _keyFrames = new KeyFrames<{a.Type}>();
        private protected override IKeyFrames KeyFrames => _keyFrames;

        public void InsertKeyFrame(float normalizedProgressKey, {a.Type} value, Cornerstone.Presentation.Animation.Easings.IEasing easingFunction)
        {{
            _keyFrames.Insert(normalizedProgressKey, value, easingFunction);
        }}
        
        public void InsertKeyFrame(float normalizedProgressKey, {a.Type} value)
        {{
            _keyFrames.Insert(normalizedProgressKey, value, Compositor.DefaultEasing);
        }}
    }}

    public partial class Compositor
    {{
        public {name}KeyFrameAnimation Create{name}KeyFrameAnimation() => new {name}KeyFrameAnimation(this);
    }}
";
            }

            code += "}";
            _output.AddSource("CompositionAnimations.cs", code);
        }
    }
}
