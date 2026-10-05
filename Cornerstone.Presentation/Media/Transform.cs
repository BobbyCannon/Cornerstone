using System;
using Cornerstone.Presentation.Animation;
using Cornerstone.Presentation.Animation.Animators;
using Cornerstone.Presentation.Media.Immutable;
using Cornerstone.Presentation.Rendering.Composition;
using Cornerstone.Presentation.Rendering.Composition.Drawing;
using Cornerstone.Presentation.Rendering.Composition.Server;
using Cornerstone.Presentation.Rendering.Composition.Transport;
using Cornerstone.Presentation.VisualTree;

namespace Cornerstone.Presentation.Media
{
    /// <summary>
    /// Represents a transform on an <see cref="Visual"/>.
    /// </summary>
    public abstract class Transform : Animatable, IMutableTransform, ICompositionRenderResource<ITransform>, ICompositorSerializable
    {
        internal Transform()
        {
            
        }

        /// <summary>
        /// Raised when the transform changes.
        /// </summary>
        public event EventHandler? Changed;

        /// <summary>
        /// Gets the transform's <see cref="Matrix"/>.
        /// </summary>
        public abstract Matrix Value { get; }

        /// <summary>
        /// Parses a <see cref="Transform"/> string.
        /// </summary>
        /// <param name="s">Six comma-delimited double values that describe the new <see cref="Transform"/>. For details check <see cref="Matrix.Parse(string)"/> </param>
        /// <returns>The <see cref="Transform"/>.</returns>
        public static Transform Parse(string s)
        {
            return new MatrixTransform(Matrix.Parse(s));
        }

        /// <summary>
        /// Raises the <see cref="Changed"/> event.
        /// </summary>
        protected void RaiseChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Converts a transform to an immutable transform.
        /// </summary>
        /// <returns>The immutable transform</returns>
        public ImmutableTransform ToImmutable()
        {
            return new ImmutableTransform(this.Value);
        }

        /// <summary>
        /// Returns a String representing this transform matrix instance.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }

        private CompositorResourceHolder<ServerCompositionSimpleTransform> _resource;
        ITransform ICompositionRenderResource<ITransform>.GetForCompositor(Compositor c) => _resource.GetForCompositor(c);
        SimpleServerObject? ICompositorSerializable.TryGetServer(Compositor c) => _resource.TryGetForCompositor(c);
        
        void ICompositionRenderResource.AddRefOnCompositor(Compositor c)
        {
            _resource.CreateOrAddRef(c, this, out _, static (cc) => new ServerCompositionSimpleTransform(cc.Server));
        }

        void ICompositionRenderResource.ReleaseOnCompositor(Compositor c) => _resource.Release(c);
        
        void ICompositorSerializable.SerializeChanges(Compositor c, BatchStreamWriter writer) =>
            ServerCompositionSimpleTransform.SerializeAllChanges(writer, Value);
    }
}
