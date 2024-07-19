#region References

using Avalonia.Controls;
using Avalonia.Interactivity;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Controls;

#endregion

namespace Avalonia.Sample.Tabs;

public partial class TabIcons : CornerstoneUserControl<TabIconsModel>
{
	#region Constants

	public const string HeaderName = "Icons";

	#endregion

	#region Constructors

	public TabIcons() : this(GetService<TabIconsModel>())
	{
	}

	public TabIcons(TabIconsModel model)
	{
		DataContext = model;
		InitializeComponent();
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override void OnLoaded(RoutedEventArgs e)
	{
		ViewModel ??= CornerstoneApplication.GetService<TabIconsModel>();
		_ = ViewModel.LoadAsync();

		if (Design.IsDesignMode)
		{
			ViewModel.SearchFilter = "This is a test";
		}

		base.OnLoaded(e);
	}

	/// <inheritdoc />
	protected override void OnUnloaded(RoutedEventArgs e)
	{
		ViewModel.Save();
		base.OnUnloaded(e);
	}

	#endregion
}