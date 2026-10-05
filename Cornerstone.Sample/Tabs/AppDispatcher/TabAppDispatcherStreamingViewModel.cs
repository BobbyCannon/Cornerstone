#region References

using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls.Text;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Sample.Tabs.AppDispatcher;

/// <summary>
/// Drains TextIngress into a TextEditorViewModel while the Streaming view is attached.
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
public partial class TabAppDispatcherStreamingViewModel : DispatchableViewModel
{
	#region Constructors

	[DependencyInjectionConstructor]
	public TabAppDispatcherStreamingViewModel(TextIngress streamModel)
	{
		Model = streamModel;
		Editor = new TextEditorViewModel();

		// Materialize span before leaving the ingress drain callback.
		TrackIngress(streamModel, span => Editor.Append(span.ToString()));
	}

	#endregion

	#region Properties

	public TextEditorViewModel Editor { get; }

	public TextIngress Model { get; }

	#endregion
}