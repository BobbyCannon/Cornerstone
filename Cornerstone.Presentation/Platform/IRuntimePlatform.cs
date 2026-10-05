using System;
using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Platform
{
    [PrivateApi]
    public interface IRuntimePlatform
    {
        RuntimePlatformInfo GetRuntimeInfo();
    }

    [PrivateApi]
    public record struct RuntimePlatformInfo
    {
        public FormFactorType FormFactor => IsDesktop ? FormFactorType.Desktop :
            IsMobile ? FormFactorType.Mobile : IsTV ? FormFactorType.TV : FormFactorType.Unknown;
        public bool IsDesktop { get; set; }
        public bool IsMobile { get; set; }
        public bool IsTV { get; set; }
    }

    public enum FormFactorType
    {
        Unknown,
        Desktop,
        Mobile,
        TV
    }
}
