#region References

using Cornerstone.Presentation.Remote.Protocol;

#endregion

namespace Cornerstone.VisualStudio.Protocol;

[PresentationRemoteMessageGuid("c1a0b7e2-6d44-4a1e-9f30-7b2e5d8a11c4")]
public class GetCompletionsRequestMessage : IEditorCall
{
	#region Constructors

	public GetCompletionsRequestMessage()
	{
		Text = string.Empty;
		AssemblyName = string.Empty;
		AssemblyPaths = new System.Collections.Generic.List<string>();
		ExtraClassNames = new System.Collections.Generic.List<string>();
	}

	#endregion

	#region Properties

	public string AssemblyName { get; set; }

	public System.Collections.Generic.List<string> AssemblyPaths { get; set; }

	public int Caret { get; set; }

	public System.Collections.Generic.List<string> ExtraClassNames { get; set; }

	public int RequestId { get; set; }

	public string Text { get; set; }

	#endregion
}