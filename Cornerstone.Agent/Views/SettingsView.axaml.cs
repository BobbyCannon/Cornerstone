#region References

using Avalonia;
using Cornerstone.Avalonia;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Agent.Views;

[SourceReflection]
public partial class SettingsView : CornerstoneUserControl<SettingsViewModel>
{
	#region Constructors

	public SettingsView()
	{
		InitializeComponent();
	}

	#endregion

	#region Methods

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		// Refresh view settings before displaying?
		// todo: automate this?
		ViewModel.UpdateWith(ViewModel.State.Settings);
		base.OnAttachedToVisualTree(e);
	}

	#endregion
}