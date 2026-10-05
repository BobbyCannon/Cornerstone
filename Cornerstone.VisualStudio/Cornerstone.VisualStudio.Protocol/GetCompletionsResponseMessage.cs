#region References

using System.Collections.Generic;
using Cornerstone.Presentation.Remote.Protocol;

#endregion

namespace Cornerstone.VisualStudio.Protocol;

[PresentationRemoteMessageGuid("8e4f2c91-0b57-4d6a-a813-55c0e9d27f60")]
public class GetCompletionsResponseMessage : IEditorCall
{
	#region Constructors

	public GetCompletionsResponseMessage()
	{
		DisplayTexts = new List<string>();
		Items = new List<CompletionItemMessage>();
		Error = string.Empty;
	}

	#endregion

	#region Properties

	public List<string> DisplayTexts { get; set; }

	public string Error { get; set; }

	public int RequestId { get; set; }

	public List<CompletionItemMessage> Items { get; set; }

	public int StartPosition { get; set; }

	#endregion
}