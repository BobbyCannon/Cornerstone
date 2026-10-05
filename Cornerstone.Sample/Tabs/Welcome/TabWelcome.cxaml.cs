#region References

using System;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Sample.Tabs.AppDispatcher;
using Cornerstone.Sample.Tabs.Controls;
using Cornerstone.Sample.Tabs.Data;
using Cornerstone.Sample.Tabs.Documentation;
using Cornerstone.Sample.Tabs.Inputs;
using Cornerstone.Sample.Tabs.Keystone;
using Cornerstone.Sample.Tabs.Maps;
using Cornerstone.Sample.Tabs.Media;
using Cornerstone.Sample.Tabs.Observe;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Sample.Tabs.Welcome;

[SourceReflection]
public partial class TabWelcome : UserControl
{
	#region Constants

	public const string HeaderName = "Welcome";

	#endregion

	#region Fields

	private readonly AppViewModel _appViewModel;
	private NavigationMenu _menu;

	#endregion

	#region Constructors

	public TabWelcome() : this(
		AppBootstrap.GetInstance<AppViewModel>(),
		AppBootstrap.GetInstance<IRuntimeInformation>())
	{
	}

	[DependencyInjectionConstructor]
	public TabWelcome(AppViewModel appViewModel, IRuntimeInformation runtimeInformation)
	{
		_appViewModel = appViewModel;
		RuntimeInformation = runtimeInformation;
		DataContext = this;
		InitializeComponent();

		var pending = _appViewModel.TakePendingSection();
		if (pending is not null)
		{
			ShowSection(pending);
		}
	}

	#endregion

	#region Properties

	public IRuntimeInformation RuntimeInformation { get; }

	#endregion

	#region Methods

	public static Type MatchSavedSection(string saved)
	{
		if (string.IsNullOrEmpty(saved))
		{
			return null;
		}

		Type[] sections =
		[
			typeof(TabWelcome),
			typeof(TabThemes),
			typeof(TabKeystone),
			typeof(TabAppDispatcherDemos),
			typeof(TabControls),
			typeof(TabInputs),
			typeof(TabData),
			typeof(TabMaps),
			typeof(TabMedia),
			typeof(TabObserve),
			typeof(TabDocumentation)
		];

		foreach (var section in sections)
		{
			if (section.ToAssemblyName() == saved)
			{
				return section;
			}
		}

		return null;
	}

	public void ShowSection(Type sectionType)
	{
		if ((sectionType is null) || (sectionType == typeof(TabWelcome)))
		{
			Pages.GoHome();
			return;
		}

		string title;
		Control page;
		if (sectionType == typeof(TabThemes))
		{
			title = TabThemes.HeaderName;
			page = new TabThemes();
		}
		else if (sectionType == typeof(TabKeystone))
		{
			title = TabKeystone.HeaderName;
			page = new TabKeystone();
		}
		else if (sectionType == typeof(TabAppDispatcherDemos))
		{
			title = TabAppDispatcherDemos.HeaderName;
			page = new TabAppDispatcherDemos();
		}
		else if (sectionType == typeof(TabControls))
		{
			title = TabControls.HeaderName;
			page = new TabControls();
		}
		else if (sectionType == typeof(TabInputs))
		{
			title = TabInputs.HeaderName;
			page = new TabInputs();
		}
		else if (sectionType == typeof(TabData))
		{
			title = TabData.HeaderName;
			page = new TabData();
		}
		else if (sectionType == typeof(TabMaps))
		{
			title = TabMaps.HeaderName;
			page = new TabMaps();
		}
		else if (sectionType == typeof(TabMedia))
		{
			title = TabMedia.HeaderName;
			page = new TabMedia();
		}
		else if (sectionType == typeof(TabObserve))
		{
			title = TabObserve.HeaderName;
			page = new TabObserve();
		}
		else if (sectionType == typeof(TabDocumentation))
		{
			title = TabDocumentation.HeaderName;
			page = new TabDocumentation();
		}
		else
		{
			return;
		}

		Pages.GoHome();
		Pages.Navigate(title, page);
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		_menu = this.FindAncestorOfType<NavigationMenu>();
		Pages.PropertyChanged += OnPagesPropertyChanged;
		SyncMenuButton();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		Pages.PropertyChanged -= OnPagesPropertyChanged;
		if (_menu is not null)
		{
			_menu.ShowMenuButton = true;
			_menu = null;
		}

		base.OnDetachedFromVisualTree(e);
	}

	private void OnPagesPropertyChanged(object sender, PresentationPropertyChangedEventArgs change)
	{
		if (change.Property.Name == nameof(PageNavigator.CanGoBack))
		{
			SyncMenuButton();
		}
	}

	private void SyncMenuButton()
	{
		if (_menu is null)
		{
			return;
		}

		_menu.ShowMenuButton = !Pages.CanGoBack;
	}

	private void OpenControls(object sender, RoutedEventArgs e)
	{
		Pages.Navigate(TabControls.HeaderName, new TabControls());
	}

	private void OpenDispatcher(object sender, RoutedEventArgs e)
	{
		Pages.Navigate(TabAppDispatcherDemos.HeaderName, new TabAppDispatcherDemos());
	}

	private void OpenDocumentation(object sender, RoutedEventArgs e)
	{
		Pages.Navigate(TabDocumentation.HeaderName, new TabDocumentation());
	}

	private void OpenInputs(object sender, RoutedEventArgs e)
	{
		Pages.Navigate(TabInputs.HeaderName, new TabInputs());
	}

	private void OpenKeystone(object sender, RoutedEventArgs e)
	{
		Pages.Navigate(TabKeystone.HeaderName, new TabKeystone());
	}

	private void OpenMaps(object sender, RoutedEventArgs e)
	{
		Pages.Navigate(TabMaps.HeaderName, new TabMaps());
	}

	private void OpenMedia(object sender, RoutedEventArgs e)
	{
		Pages.Navigate(TabMedia.HeaderName, new TabMedia());
	}

	private void OpenObserve(object sender, RoutedEventArgs e)
	{
		Pages.Navigate(TabObserve.HeaderName, new TabObserve());
	}

	private void OpenSync(object sender, RoutedEventArgs e)
	{
		Pages.Navigate(TabData.HeaderName, new TabData());
	}

	private void OpenThemes(object sender, RoutedEventArgs e)
	{
		Pages.Navigate(TabThemes.HeaderName, new TabThemes());
	}

	#endregion
}