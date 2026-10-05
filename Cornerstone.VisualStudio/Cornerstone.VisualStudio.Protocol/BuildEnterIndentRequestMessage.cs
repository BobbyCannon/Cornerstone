using Cornerstone.Presentation.Remote.Protocol;

namespace Cornerstone.VisualStudio.Protocol
{
	[PresentationRemoteMessageGuid("4d6a1c88-9e20-4f55-b7d3-2a91c0e64b17")]
	public class BuildEnterIndentRequestMessage : IEditorCall
	{
		public BuildEnterIndentRequestMessage()
		{
			Line = string.Empty;
			PreviousLine = string.Empty;
			NewLine = string.Empty;
		}

		public string Line { get; set; }

		public int CaretIndex { get; set; }

		public string PreviousLine { get; set; }

		public int IndentSize { get; set; }

		public string NewLine { get; set; }

		public int RequestId { get; set; }
	}
}
