using System;

namespace Cornerstone.Presentation.Media.Fonts
{
    internal class EmptySystemFontCollection : FontCollectionBase
    {
        public override Uri Key => FontManager.SystemFontsKey;
    }
}
