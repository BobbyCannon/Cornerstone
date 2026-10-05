using System.Collections.Generic;
using Cornerstone.Presentation.Remote.Protocol;

namespace Cornerstone.VisualStudio.Protocol
{
	[PresentationRemoteMessageGuid("a1c4e8b2-6d47-4f19-9c30-5b7e2d84a6f1")]
	public class EnsureMetadataRequestMessage : IEditorCall
	{
		public EnsureMetadataRequestMessage()
		{
			AssemblyPaths = new List<string>();
			SolutionDirectory = string.Empty;
			TargetPaths = new List<string>();
		}

		public List<string> AssemblyPaths { get; set; }

		public List<string> TargetPaths { get; set; }

		public int RequestId { get; set; }

		public string SolutionDirectory { get; set; }
	}
}
