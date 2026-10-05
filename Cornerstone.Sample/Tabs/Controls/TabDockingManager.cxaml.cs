#region References

using System;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Controls.DockingManager;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Text;

#endregion

namespace Cornerstone.Sample.Tabs.Controls;

[SourceReflection]
public partial class TabDockingManager : UserControl
{
	#region Constants

	public const string HeaderName = "Docking Manager";

	#endregion

	#region Fields

	private int _tabIndex;

	#endregion

	#region Constructors

	public TabDockingManager() : this(AppBootstrap.GetInstance<DockingManager>())
	{
	}

	[DependencyInjectionConstructor]
	public TabDockingManager(DockingManager dockingManager)
	{
		_tabIndex = 0;

		DockingManager = dockingManager;
		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	public DockingManager DockingManager { get; }

	#endregion

	#region Methods

	protected override void OnInitialized()
	{
		DockingManager.Initialize([]);
		DockingManager.NewTabCommand = NewTabRequestedCommand;
		base.OnInitialized();
	}

	protected override void OnLoaded(RoutedEventArgs e)
	{
		DockingManager.Add(GetNewTabModel());
		base.OnLoaded(e);
	}

	protected override void OnUnloaded(RoutedEventArgs e)
	{
		DockingManager.NewTabCommand = null;
		DockingManager.Uninitialize();
		base.OnUnloaded(e);
	}

	private DockableTabModel GetNewTabModel()
	{
		_tabIndex++;
		var response = new TextTabViewModel
		{
			Header = $"Tab: {_tabIndex}",
			Text = $"Content: {_tabIndex}"
		};
		response.InitializeLifecycle();
		response.LoadLifecycle();
		response.StartLifecycle();
		return response;
	}

	[RelayCommand]
	private void NewTabRequested(DockingTabControl e)
	{
		e.Add(GetNewTabModel());
	}

	#endregion
}

[SourceReflection]
public partial class TextTabViewModel : DocumentTabModel
{
	#region Constructors

	public TextTabViewModel()
		: base(Guid.NewGuid(), "Text", "Icons.File")
	{
	}

	protected TextTabViewModel(Guid id, string header, string iconName)
		: base(id, header, iconName)
	{
	}

	#endregion

	#region Properties

	public TabDockingManager Manager { get; set; }

	public string Text { get; set; }

	#endregion
}

[SourceReflection]
public class TextTabView : ContentControl
{
	#region Methods

	/// <inheritdoc />
	[UnconditionalSuppressMessage("Aot", "IL3050", Justification = "Sample docking demo binds TextTabViewModel by name.")]
	[UnconditionalSuppressMessage("Trim", "IL2026", Justification = "Sample docking demo binds TextTabViewModel by name.")]
	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		var textBlock = new TextBlock();
		textBlock.Bind(TextBlock.TextProperty, new Binding(nameof(TextTabViewModel.Text)));

		var textBlock2 = new TextBlock();
		textBlock2.Bind(TextBlock.TextProperty, new Binding(nameof(TextTabViewModel.IsSelected)));

		var stackPanel = new StackPanel
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(20),
			Spacing = 10
		};
		stackPanel.Children.Add(textBlock);
		stackPanel.Children.Add(textBlock2);

		Content = stackPanel;

		base.OnAttachedToVisualTree(e);
	}

	#endregion
}