using Cornerstone.Presentation.Remote.Protocol;

namespace Cornerstone.VisualStudio.Protocol
{
	[PresentationRemoteMessageGuid("b83e05d4-71c6-4a90-8f22-6d4e9a10c5b8")]
	public class BuildEnterIndentResponseMessage : IEditorCall
	{
		public BuildEnterIndentResponseMessage()
		{
			Insertion = string.Empty;
			Error = string.Empty;
		}

		public string Insertion { get; set; }

		public string Error { get; set; }

		public int RequestId { get; set; }
	}
}
