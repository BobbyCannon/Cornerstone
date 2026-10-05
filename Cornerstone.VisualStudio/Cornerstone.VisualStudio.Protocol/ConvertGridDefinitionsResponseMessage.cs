using Cornerstone.Presentation.Remote.Protocol;

namespace Cornerstone.VisualStudio.Protocol
{
	[PresentationRemoteMessageGuid("6b0d3e84-2f71-4c95-a148-9e5c7d20b6a3")]
	public class ConvertGridDefinitionsResponseMessage : IEditorCall
	{
		public ConvertGridDefinitionsResponseMessage()
		{
			AttributeName = string.Empty;
			AttributeValue = string.Empty;
			DisplayText = string.Empty;
			Error = string.Empty;
			Insertion = string.Empty;
		}

		public string AttributeName { get; set; }

		public string AttributeValue { get; set; }

		public int CanConvert { get; set; }

		public string DisplayText { get; set; }

		public string Error { get; set; }

		public string Insertion { get; set; }

		public int InsertAt { get; set; }

		public int RemoveLength { get; set; }

		public int RemoveStart { get; set; }

		public int RequestId { get; set; }
	}
}
