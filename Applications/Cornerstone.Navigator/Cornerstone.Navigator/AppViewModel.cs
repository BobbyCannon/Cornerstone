#region References

using System;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Navigator.Keystone;
using Cornerstone.Navigator.Keystone.State;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Web;

#endregion

namespace Cornerstone.Navigator;

[SourceReflection]
[Notifiable(["*"])]
public partial class AppViewModel : ApplicationViewModel
{
	#region Fields

	private BrowserFavorite _editingFavorite;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public AppViewModel(
		AppState state,
		IDependencyProvider dependencyProvider,
		IDispatcher dispatcher)
		: base(dependencyProvider, dispatcher)
	{
		State = state;
		Address = BrowserAddress.HomeUri;
		_editingFavorite = null;
		EditorName = string.Empty;
		EditorUri = string.Empty;
		IsSecure = false;
		LocationHost = string.Empty;
		PageTitle = BrowserChrome.DefaultPageTitle;
		SecurityText = string.Empty;
		StatusText = BrowserChrome.StatusText(false, string.Empty);
		WebView = new WebView();
	}

	#endregion

	#region Properties

	[Notify]
	public partial string Address { get; set; }

	[Notify]
	public partial bool CanGoBack { get; set; }

	[Notify]
	public partial bool CanGoForward { get; set; }

	[Notify]
	public partial string EditorName { get; set; }

	[Notify]
	public partial string EditorUri { get; set; }

	public PresentationList<BrowserFavorite> Favorites => Settings.Favorites;

	[Notify]
	public partial bool HasFavorites { get; set; }

	[Notify]
	public partial bool IsFavorite { get; set; }

	[Notify]
	public partial bool IsFavoriteEditorOpen { get; set; }

	[Notify]
	public partial bool IsNavigating { get; set; }

	[Notify]
	public partial bool IsSecure { get; set; }

	[Notify]
	public partial string LocationHost { get; set; }

	[Notify]
	public partial string PageTitle { get; set; }

	[Notify]
	public partial string SecurityText { get; set; }

	public AppSettings Settings => State.Settings;

	public AppState State { get; }

	[Notify]
	public partial string StatusText { get; set; }

	public WebView WebView { get; }

	#endregion

	#region Methods

	public override void InitializeLifecycle()
	{
		WebView.NavigationStarted += OnNavigationStarted;
		WebView.NavigationCompleted += OnNavigationCompleted;
		WebView.NewWindowRequested += OnNewWindowRequested;
		WebView.PropertyChanged += OnWebViewPropertyChanged;
		Favorites.CollectionChanged += OnFavoritesChanged;
		base.InitializeLifecycle();
	}

	public override void LoadLifecycle()
	{
		base.LoadLifecycle();
		HasFavorites = Favorites.Count > 0;
	}

	[RelayCommand]
	public void Navigate()
	{
		WebView.Navigate(BrowserAddress.Resolve(Address));
	}

	public override void StartLifecycle()
	{
		base.StartLifecycle();
		State.Settings.ApplyTheme();
		WebView.Navigate(BrowserAddress.HomeUri);
		SyncChrome();
	}

	public override void UninitializeLifecycle()
	{
		WebView.NavigationStarted -= OnNavigationStarted;
		WebView.NavigationCompleted -= OnNavigationCompleted;
		WebView.NewWindowRequested -= OnNewWindowRequested;
		WebView.PropertyChanged -= OnWebViewPropertyChanged;
		if (Settings.Favorites != null)
		{
			Settings.Favorites.CollectionChanged -= OnFavoritesChanged;
		}

		base.UninitializeLifecycle();
	}

	private bool CanExecuteGoBack()
	{
		return CanGoBack;
	}

	private bool CanExecuteGoForward()
	{
		return CanGoForward;
	}

	[RelayCommand]
	private void CloseFavoriteEditor()
	{
		_editingFavorite = null;
		IsFavoriteEditorOpen = false;
	}

	private string CurrentPageUri()
	{
		if (WebView.Uri != null)
		{
			return WebView.Uri.OriginalString;
		}

		return Address;
	}

	[RelayCommand]
	private void EditFavorite(BrowserFavorite favorite)
	{
		if (favorite == null)
		{
			return;
		}

		_editingFavorite = favorite;
		EditorName = favorite.Name;
		EditorUri = favorite.Uri;
		IsFavoriteEditorOpen = true;
	}

	private BrowserFavorite FindFavorite(string uri)
	{
		if (string.IsNullOrWhiteSpace(uri))
		{
			return null;
		}

		return Favorites.FirstOrDefault(x => string.Equals(x.Uri, uri, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(x.Uri.TrimEnd('/'), uri.TrimEnd('/'), StringComparison.OrdinalIgnoreCase));
	}

	[RelayCommand(CanExecuteMethod = nameof(CanExecuteGoBack))]
	private void GoBack()
	{
		if (!CanGoBack)
		{
			return;
		}

		WebView.GoBack();
	}

	[RelayCommand(CanExecuteMethod = nameof(CanExecuteGoForward))]
	private void GoForward()
	{
		if (!CanGoForward)
		{
			return;
		}

		WebView.GoForward();
	}

	[RelayCommand]
	private void GoHome()
	{
		WebView.Navigate(BrowserAddress.HomeUri);
	}

	[RelayCommand]
	private void NavigateFavorite(BrowserFavorite favorite)
	{
		if (favorite == null)
		{
			return;
		}

		Address = favorite.Uri;
		Navigate();
		IsFavoriteEditorOpen = false;
	}

	private void OnFavoritesChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		HasFavorites = Favorites.Count > 0;
		IsFavorite = FindFavorite(CurrentPageUri()) != null;
	}

	private void OnNavigationCompleted(object sender, WebViewNavigationEventArgs e)
	{
		SyncChrome();
	}

	private void OnNavigationStarted(object sender, WebViewNavigationEventArgs e)
	{
		IsNavigating = true;
		LocationHost = BrowserChrome.HostFromUri(Address);
		StatusText = BrowserChrome.StatusText(true, LocationHost);
	}

	private void OnNewWindowRequested(object sender, WebViewNewWindowEventArgs e)
	{
		if (e.Uri == null)
		{
			return;
		}

		e.Handled = true;
		WebView.Navigate(e.Uri);
	}

	private void OnWebViewPropertyChanged(object sender, PresentationPropertyChangedEventArgs e)
	{
		SyncChrome();
	}

	[RelayCommand]
	private void OpenFavoriteEditor()
	{
		var existing = FindFavorite(CurrentPageUri());
		if (existing != null)
		{
			EditFavorite(existing);
			return;
		}

		_editingFavorite = null;
		EditorName = PageTitle;
		EditorUri = Address;
		IsFavoriteEditorOpen = true;
	}

	[RelayCommand]
	private void Reload()
	{
		WebView.Reload();
	}

	[RelayCommand]
	[UnconditionalSuppressMessage("Aot", "IL3050", Justification = "SettingsManager.Save serializes Navigator favorites.")]
	[UnconditionalSuppressMessage("Trim", "IL2026", Justification = "SettingsManager.Save serializes Navigator favorites.")]
	private void RemoveFavorite(BrowserFavorite favorite)
	{
		if (favorite != null)
		{
			Favorites.Remove(favorite);
			Settings.Save();
		}
	}

	[RelayCommand]
	[UnconditionalSuppressMessage("Aot", "IL3050", Justification = "SettingsManager.Save serializes Navigator favorites.")]
	[UnconditionalSuppressMessage("Trim", "IL2026", Justification = "SettingsManager.Save serializes Navigator favorites.")]
	private void SaveFavorite()
	{
		var uri = BrowserAddress.Resolve(EditorUri);
		if (string.IsNullOrWhiteSpace(uri))
		{
			return;
		}

		var name = string.IsNullOrWhiteSpace(EditorName) ? uri : EditorName.Trim();
		if (_editingFavorite != null)
		{
			_editingFavorite.Name = name;
			_editingFavorite.Uri = uri;
		}
		else
		{
			var existing = FindFavorite(uri);
			if (existing != null)
			{
				existing.Name = name;
				existing.Uri = uri;
			}
			else
			{
				Favorites.Add(new BrowserFavorite(name, uri));
			}
		}

		_editingFavorite = null;
		IsFavoriteEditorOpen = false;
		Settings.Save();
	}

	[RelayCommand]
	private void Stop()
	{
		WebView.Stop();
	}

	private void SyncChrome()
	{
		CanGoBack = WebView.CanGoBack;
		CanGoForward = WebView.CanGoForward;
		GoBackCommand.Refresh();
		GoForwardCommand.Refresh();
		IsNavigating = WebView.IsNavigating;
		if (WebView.Uri != null)
		{
			Address = WebView.Uri.OriginalString;
		}

		PageTitle = BrowserChrome.PageTitle(WebView.Title);
		IsFavorite = FindFavorite(CurrentPageUri()) != null;
		LocationHost = BrowserChrome.HostFromUri(Address);
		IsSecure = BrowserChrome.IsSecure(Address);
		SecurityText = BrowserChrome.SecurityText(Address);
		StatusText = BrowserChrome.StatusText(IsNavigating, LocationHost);
	}

	#endregion
}