#region References

using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Presentation;

#endregion

namespace Avalonia.Sample.Tabs;

public partial class TabComboBox : CornerstoneUserControl<TabComboBoxViewModel>
{
	#region Constants

	public const string HeaderName = "ComboBox";

	#endregion

	#region Constructors

	public TabComboBox()
	{
		InitializeComponent();
		ViewModel = new TabComboBoxViewModel();
		DataContext = ViewModel;
	}

	#endregion

	#region Methods

	private void InitializeComponent()
	{
		AvaloniaXamlLoader.Load(this);

		var fontComboBox = this.Get<ComboBox>("FontComboBox");
		fontComboBox.ItemsSource = FontManager.Current.SystemFonts;
		fontComboBox.SelectedIndex = 0;
	}

	#endregion
}

public class TabComboBoxViewModel : ViewModel
{
	#region Fields

	private bool _wrapSelection;

	#endregion

	#region Constructors

	public TabComboBoxViewModel()
	{
		Values =
		[
			new IdAndName { Id = "Id 1", Name = "Name 1" },
			new IdAndName { Id = "Id 2", Name = "Name 2" },
			new IdAndName { Id = "Id 3", Name = "Name 3" },
			new IdAndName { Id = "Id 4", Name = "Name 4" },
			new IdAndName { Id = "Id 5", Name = "Name 5" }
		];
	}

	#endregion

	#region Properties

	public ObservableCollection<IdAndName> Values { get; set; }

	public bool WrapSelection
	{
		get => _wrapSelection;
		set => SetProperty(ref _wrapSelection, value);
	}

	#endregion
}

public class IdAndName
{
	#region Properties

	public string Id { get; set; }
	public string Name { get; set; }

	#endregion
}