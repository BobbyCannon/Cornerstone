using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Cornerstone.Presentation.Markup.Xaml
{
    [DataContract]
    class CornerstoneResourceXamlInfo
    {
        [DataMember]
        public Dictionary<string, string> ClassToResourcePathIndex { get; set; } = new Dictionary<string, string>();
    }
}
