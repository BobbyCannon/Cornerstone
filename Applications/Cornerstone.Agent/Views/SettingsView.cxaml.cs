#region References

using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Theme;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Agent.Views;

[SourceReflection]
public partial class SettingsView : UserControl<SettingsViewModel>
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