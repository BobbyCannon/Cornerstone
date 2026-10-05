using Cornerstone.Presentation.Remote.Protocol;

namespace Cornerstone.VisualStudio.Protocol
{
	[PresentationRemoteMessageGuid("b8e2c4d1-3a56-4e90-8f17-6c0d9b35e2a4")]
	public class EnsureMetadataResponseMessage : IEditorCall
	{
		public EnsureMetadataResponseMessage()
		{
			Error = string.Empty;
		}

		public string Error { get; set; }

		public int FromCache { get; set; }

		public int ReadCount { get; set; }

		public int RequestId { get; set; }

		public double Seconds { get; set; }
	}
}
