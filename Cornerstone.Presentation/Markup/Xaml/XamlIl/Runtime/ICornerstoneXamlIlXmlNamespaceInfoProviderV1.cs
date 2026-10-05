using System.Collections.Generic;

namespace Cornerstone.Presentation.Markup.Xaml.XamlIl.Runtime
{
    public interface ICornerstoneXamlIlXmlNamespaceInfoProvider
    {
        IReadOnlyDictionary<string, IReadOnlyList<CornerstoneXamlIlXmlNamespaceInfo>> XmlNamespaces { get; }
    }
    
    public class CornerstoneXamlIlXmlNamespaceInfo
    {
        public string ClrNamespace { get; set; } = string.Empty;
        public string ClrAssemblyName { get; set; } = string.Empty;
    }
}
