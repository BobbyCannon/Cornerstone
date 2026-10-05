using Cornerstone.Presentation.Remote.Protocol;

namespace Cornerstone.VisualStudio.Protocol
{
	[PresentationRemoteMessageGuid("e7a52b18-4c90-4d63-b8f1-2a6e0d94c573")]
	public class LookupNamespaceResponseMessage : IEditorCall
	{
		public const int KindNone = 0;
		public const int KindAddNamespaceAndAlias = 1;
		public const int KindUseAlias = 2;
		public const int KindAddNamespace = 3;

		public LookupNamespaceResponseMessage()
		{
			Alias = string.Empty;
			Error = string.Empty;
			XmlNamespace = string.Empty;
		}

		public string Alias { get; set; }

		public string Error { get; set; }

		public int Kind { get; set; }

		public int RequestId { get; set; }

		public string XmlNamespace { get; set; }
	}
}
