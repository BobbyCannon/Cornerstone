using System;
using Cornerstone.Presentation;

namespace Cornerstone.Presentation.Media.Fonts
{
    public static class SystemFontAppBuilderExtension
    {
        public static AppBuilder WithSystemFontSource(this AppBuilder appBuilder, Uri fontSource)
        {
            return appBuilder.ConfigureFonts(fontManager =>
            {
                if(fontManager.SystemFonts is SystemFontCollection systemFontCollection)
                {
                    systemFontCollection.TryAddFontSource(fontSource);
                }
            });
        }
    }
}
