#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.DockingManager;
using Cornerstone.Presentation;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Sample.Tabs;

[SourceReflection]
public partial class TabDockingManager : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Docking Manager";

	#endregion

	#region Fields

	private int _tabIndex;

	#endregion

	#region Constructors

	public TabDockingManager()
	{
		_tabIndex = 0;
		DataContext = this;
		InitializeComponent();
	}

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
		DockingManager.Add(GetNewTabModel());
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
		return new TextTabViewModel
		{
			Header = $"Tab: {_tabIndex}",
			Text = $"Content: {_tabIndex}"
		};
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
public class TextTabView : CornerstoneContentControl
{
	#region Methods

	/// <inheritdoc />
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