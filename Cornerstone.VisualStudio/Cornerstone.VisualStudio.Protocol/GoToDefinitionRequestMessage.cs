using System.Collections.Generic;
using Cornerstone.Presentation.Remote.Protocol;

namespace Cornerstone.VisualStudio.Protocol
{
	[PresentationRemoteMessageGuid("d27a4c18-5e90-4b63-8f11-2c6a9e40b7d5")]
	public class GoToDefinitionRequestMessage : IEditorCall
	{
		public GoToDefinitionRequestMessage()
		{
			Text = string.Empty;
			AssemblyName = string.Empty;
			AssemblyPaths = new List<string>();
		}

		public string AssemblyName { get; set; }

		public List<string> AssemblyPaths { get; set; }

		public int Caret { get; set; }

		public int RequestId { get; set; }

		public string Text { get; set; }
	}
}
