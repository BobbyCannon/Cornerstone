using System.Collections.Generic;
using Cornerstone.Presentation.Remote.Protocol;

namespace Cornerstone.VisualStudio.Protocol
{
	[PresentationRemoteMessageGuid("c3d91f70-8a24-4b6e-91d5-0e7c4a18b296")]
	public class LookupNamespaceRequestMessage : IEditorCall
	{
		public LookupNamespaceRequestMessage()
		{
			AssemblyPaths = new List<string>();
			TypeName = string.Empty;
			Text = string.Empty;
			Alias = string.Empty;
		}

		public string Alias { get; set; }

		public List<string> AssemblyPaths { get; set; }

		public bool HasAlias { get; set; }

		public int RequestId { get; set; }

		public string Text { get; set; }

		public string TypeName { get; set; }
	}
}
