using System;
using Cornerstone.Presentation.Media.TextFormatting;

namespace Cornerstone.Presentation.Media
{
    internal sealed class TextNoneTrimming : TextTrimming
    {
        public override TextCollapsingProperties CreateCollapsingProperties(TextCollapsingCreateInfo createInfo)
        {
            throw new NotSupportedException();
        }

        public override string ToString()
        {
            return nameof(None);
        }
    }
}
