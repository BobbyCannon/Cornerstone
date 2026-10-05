using Cornerstone.Presentation.Platforms.MacOS.Interop;

namespace Cornerstone.Presentation.Platforms.MacOS
{
    internal class EmbeddableTopLevelImpl : TopLevelImpl
    {
        public EmbeddableTopLevelImpl(ICornerstoneNativeFactory factory) : base(factory)
        {
            using (var e = new TopLevelEvents(this))
            {
                Init(new MacOSTopLevelHandle(factory.CreateTopLevel(e)));
            }
        }
    }
}
