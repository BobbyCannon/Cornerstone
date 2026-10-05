using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Media.Fonts;

namespace Cornerstone.Presentation.Media
{
    internal class CompositeFontFamilyKey : FontFamilyKey
    {
        public CompositeFontFamilyKey(Uri source, FontFamilyKey[] keys) : base(source, null)
        {
            Keys = keys;
        }

        public IReadOnlyList<FontFamilyKey> Keys { get; }
    }
}
