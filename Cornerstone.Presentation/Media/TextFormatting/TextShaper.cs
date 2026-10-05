using System;
using Cornerstone.Presentation.Platform;

namespace Cornerstone.Presentation.Media.TextFormatting
{
    /// <summary>
    /// A class that is responsible for text shaping.
    /// </summary>
    public class TextShaper
    {
        private readonly ITextShaperImpl _platformImpl;

        public TextShaper(ITextShaperImpl platformImpl)
        {
            _platformImpl = platformImpl;
        }

        /// <summary>
        /// Gets the current text shaper.
        /// </summary>
        public static TextShaper Current
        {
            get
            {
                var current = PresentationLocator.Current.GetService<TextShaper>();

                if (current != null)
                {
                    return current;
                }

                var textShaperImpl = PresentationLocator.Current.GetRequiredService<ITextShaperImpl>();

                current = new TextShaper(textShaperImpl);

                PresentationLocator.CurrentMutable.Bind<TextShaper>().ToConstant(current);

                return current;
            }
        }

        /// <inheritdoc cref="ITextShaperImpl.ShapeText"/>
        public ShapedBuffer ShapeText(ReadOnlyMemory<char> text, TextShaperOptions options = default)
        {
            return _platformImpl.ShapeText(text, options);
        }

        public ShapedBuffer ShapeText(string text, TextShaperOptions options = default)
        {
            return ShapeText(text.AsMemory(), options);
        }
    }
}
