using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Xml;

namespace Cornerstone.Presentation.Remote.Protocol.Designer
{
    [PresentationRemoteMessageGuid("9AEC9A2E-6315-4066-B4BA-E9A9EFD0F8CC")]
    public class UpdateXamlMessage
    {
        public string Xaml { get; set; }
        public string AssemblyPath { get; set; }
        public string XamlFileProjectPath { get; set; }

        /// <summary>
        /// Forced preview theme: Default, Light, or Dark. Empty follows the platform fallback.
        /// </summary>
        public string ThemeVariant { get; set; }

        /// <summary>
        /// Preview accent, a ThemeColor name such as Blue. Empty leaves the application theme unchanged.
        /// </summary>
        public string ThemeColor { get; set; }

        /// <summary>
        /// Preview density: Compact, Normal, or Large. Empty leaves the application theme unchanged.
        /// </summary>
        public string ThemeDensity { get; set; }
    }

    [PresentationRemoteMessageGuid("B7A70093-0C5D-47FD-9261-22086D43A2E2")]
    public class UpdateXamlResultMessage
    {
        public string Error { get; set; }
        public string Handle { get; set; }
        public ExceptionDetails Exception { get; set; }
    }

    [PresentationRemoteMessageGuid("854887CF-2694-4EB6-B499-7461B6FB96C7")]
    public class StartDesignerSessionMessage
    {
        public string SessionId { get; set; }
    }
    
    public class ExceptionDetails
    {
        public ExceptionDetails()
        {
        }

        public ExceptionDetails(Exception e)
        {
            if (e is TargetInvocationException)
            {
                e = e.InnerException;
            }

            ExceptionType = e.GetType().Name;
            Message = e.Message;

            if (e is XmlException xml)
            {
                LineNumber = xml.LineNumber;
                LinePosition = xml.LinePosition;
            }
        }

        public string ExceptionType { get; set; }
        public string Message { get; set; }
        public int? LineNumber { get; set; }
        public int? LinePosition { get; set; }
    }
}
