namespace Cornerstone.Presentation.Platforms.Windows;

internal enum BlurEffect
{
    None,
    GaussianBlur,
    Acrylic,
    MicaLight,
    MicaDark
}

internal interface ICompositionEffectsSurface
{
    bool IsBlurSupported(BlurEffect effect);
}
