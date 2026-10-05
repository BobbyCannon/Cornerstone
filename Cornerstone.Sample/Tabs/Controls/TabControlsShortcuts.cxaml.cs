#region References

using System;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Input;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Sample.Tabs.Controls;

[SourceReflection]
public partial class TabControlsShortcuts : UserControl
{
	#region Constants

	public const string HeaderName = "Shortcuts";

	#endregion

	#region Fields

	private readonly IDateTimeProvider _dateTimeProvider;

	#endregion

	#region Constructors

	public TabControlsShortcuts() : this(
		AppBootstrap.GetInstance<IDateTimeProvider>(),
		AppBootstrap.GetInstance<IRuntimeInformation>())
	{
	}

	[DependencyInjectionConstructor]
	public TabControlsShortcuts(IDateTimeProvider dateTimeProvider, IRuntimeInformation runtimeInformation)
	{
		_dateTimeProvider = dateTimeProvider;

		ShortcutBinding = new ShortcutBinding { Name = "My Command Binding" };
		ShortcutManager = new ShortcutBindingManager([ShortcutBinding]);

		RuntimeInformation = runtimeInformation;
		DataContext = this;

		InitializeComponent();
	}

	#endregion

	#region Properties

	public IRuntimeInformation RuntimeInformation { get; }

	[StyledProperty]
	public partial ShortcutBinding ShortcutBinding { get; set; }

	public ShortcutBindingManager ShortcutManager { get; }

	#endregion

	#region Methods

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		ShortcutManager.Executed += ShortcutManagerOnExecuted;
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		ShortcutManager.Executed -= ShortcutManagerOnExecuted;
		base.OnDetachedFromVisualTree(e);
	}

	private void ShortcutManagerOnExecuted(object sender, ShortcutBinding e)
	{
		Log.ViewModel.Append(_dateTimeProvider.Now.ToString("G"));
		Log.ViewModel.Append(": ");
		Log.ViewModel.Append(e.Name);
		Log.ViewModel.Append(" - ");
		Log.ViewModel.Append(e.GetDisplayString());
		Log.ViewModel.Append(Environment.NewLine);
	}

	#endregion
}
