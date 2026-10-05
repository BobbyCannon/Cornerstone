using System.Diagnostics.CodeAnalysis;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Markup.Xaml;

namespace Cornerstone.Designer.HostApp
{
    [RequiresUnreferencedCode(XamlX.TrimmingMessages.DynamicXamlReference)]
    class Program
    {
        public static void Main(string[] args)
        {
            PresentationLocator.CurrentMutable.Bind<CornerstoneXamlLoader.IRuntimeXamlLoader>()
                .ToConstant(new DesignXamlLoader());
            Cornerstone.Presentation.DesignerSupport.Remote.RemoteDesignerEntryPoint.Main(args);
        }
    }
}
