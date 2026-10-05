using System;

namespace Cornerstone.Presentation.Media.Fonts
{
    public class EmbeddedFontCollection : FontCollectionBase
    {
        private readonly Uri _key;
        private readonly Uri _source;

        public EmbeddedFontCollection(Uri key, Uri source)
        {
            _key = key;

            _source = source;

            TryAddFontSource(_source);
        }

        public override Uri Key => _key;
    }
}
