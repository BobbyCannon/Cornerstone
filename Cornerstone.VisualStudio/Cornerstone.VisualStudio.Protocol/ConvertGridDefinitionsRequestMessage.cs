using Cornerstone.Presentation.Remote.Protocol;

namespace Cornerstone.VisualStudio.Protocol
{
	[PresentationRemoteMessageGuid("91e4b7c2-5a18-4d60-8f33-0c6e2b74a1d9")]
	public class ConvertGridDefinitionsRequestMessage : IEditorCall
	{
		public ConvertGridDefinitionsRequestMessage()
		{
			Text = string.Empty;
		}

		public int Caret { get; set; }

		public int RequestId { get; set; }

		public string Text { get; set; }
	}
}
