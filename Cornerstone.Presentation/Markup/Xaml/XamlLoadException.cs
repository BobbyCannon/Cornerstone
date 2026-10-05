using System;

namespace Cornerstone.Presentation.Markup.Xaml
{
    public class XamlLoadException: Exception
    {
        public XamlLoadException()
        {
        }

        public XamlLoadException(string message): base(message)
        {
        }

        public XamlLoadException(string message, Exception innerException): base(message, innerException)
        {
        }
    }
}
