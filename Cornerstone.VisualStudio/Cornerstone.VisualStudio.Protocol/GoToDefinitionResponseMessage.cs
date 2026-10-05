using Cornerstone.Presentation.Remote.Protocol;

namespace Cornerstone.VisualStudio.Protocol
{
	[PresentationRemoteMessageGuid("f0b81e56-3c74-4a29-9d08-71e5c2a84b60")]
	public class GoToDefinitionResponseMessage : IEditorCall
	{
		public GoToDefinitionResponseMessage()
		{
			TypeFullName = string.Empty;
			MemberName = string.Empty;
			ClassName = string.Empty;
			MethodName = string.Empty;
			Error = string.Empty;
			DocumentOffset = -1;
		}

		public string ClassName { get; set; }

		public int DocumentOffset { get; set; }

		public string Error { get; set; }

		public int Kind { get; set; }

		public string MemberName { get; set; }

		public string MethodName { get; set; }

		public int RequestId { get; set; }

		public string TypeFullName { get; set; }
	}
}
