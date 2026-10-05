using Cornerstone.Presentation.Remote.Protocol;

namespace Cornerstone.VisualStudio.Protocol
{
	[PresentationRemoteMessageGuid("9c4e1a70-2b58-4d63-a1f6-8e0c5b27d349")]
	public class MetadataProgressMessage : IEditorCall
	{
		public MetadataProgressMessage()
		{
			AssemblyName = string.Empty;
			Phase = string.Empty;
			Starting = 0;
			RequestId = 0;
			Index = 0;
			Total = 0;
		}

		public string AssemblyName { get; set; }

		public int Index { get; set; }

		public string Phase { get; set; }

		public int RequestId { get; set; }

		public int Starting { get; set; }

		public int Total { get; set; }
	}
}
