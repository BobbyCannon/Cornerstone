using System;
using Cornerstone.Presentation.Media.Imaging;
using Cornerstone.Presentation.Media.Immutable;
using Cornerstone.Presentation.Rendering.Composition;
using Cornerstone.Presentation.Rendering.Composition.Server;
using Cornerstone.Presentation.Rendering.Composition.Transport;

namespace Cornerstone.Presentation.Media
{
    /// <summary>
    /// Paints an area with an <see cref="IBitmap"/>.
    /// </summary>
    public sealed class ImageBrush : TileBrush, IImageBrush, IMutableBrush
    {
        /// <summary>
        /// Defines the <see cref="Visual"/> property.
        /// </summary>
        public static readonly StyledProperty<IImageBrushSource?> SourceProperty =
            PresentationProperty.Register<ImageBrush, IImageBrushSource?>(nameof(Source));

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageBrush"/> class.
        /// </summary>
        public ImageBrush()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageBrush"/> class.
        /// </summary>
        /// <param name="source">The image to draw.</param>
        public ImageBrush(IImageBrushSource? source)
        {
            Source = source;
        }

        /// <summary>
        /// Gets or sets the image to draw.
        /// </summary>
        public IImageBrushSource? Source
        {
            get { return GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        /// <inheritdoc/>
        public IImmutableBrush ToImmutable()
        {
            return new ImmutableImageBrush(this);
        }

        internal override Func<Compositor, ServerCompositionSimpleBrush> Factory =>
            static c => new ServerCompositionSimpleImageBrush(c.Server);

        private protected override void SerializeChanges(Compositor c, BatchStreamWriter writer)
        {
            base.SerializeChanges(c, writer);
            var clonedRef = Source?.Bitmap?.Clone();
            writer.WriteObject(clonedRef);
        }
    }
}
