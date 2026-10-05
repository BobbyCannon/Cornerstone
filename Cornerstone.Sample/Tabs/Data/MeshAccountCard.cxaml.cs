#region References

using System;
using System.Windows.Input;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Sample.Tabs.Data;

/// <summary>
/// One mesh database card: title, display name, modified time, and save.
/// </summary>
[SourceReflection]
public partial class MeshAccountCard : UserControl
{
	#region Fields

	public static readonly StyledProperty<string> DisplayNameProperty;

	public static readonly StyledProperty<DateTime> ModifiedOnProperty;

	public static readonly StyledProperty<ICommand> SaveCommandProperty;

	public static readonly StyledProperty<string> TitleProperty;

	#endregion

	#region Constructors

	public MeshAccountCard()
	{
		InitializeComponent();
	}

	static MeshAccountCard()
	{
		DisplayNameProperty = PresentationProperty.Register<MeshAccountCard, string>(
			nameof(DisplayName),
			string.Empty,
			defaultBindingMode: BindingMode.TwoWay);
		ModifiedOnProperty = PresentationProperty.Register<MeshAccountCard, DateTime>(nameof(ModifiedOn));
		SaveCommandProperty = PresentationProperty.Register<MeshAccountCard, ICommand>(nameof(SaveCommand));
		TitleProperty = PresentationProperty.Register<MeshAccountCard, string>(nameof(Title), string.Empty);
	}

	#endregion

	#region Properties

	public string DisplayName
	{
		get => GetValue(DisplayNameProperty);
		set => SetValue(DisplayNameProperty, value);
	}

	public DateTime ModifiedOn
	{
		get => GetValue(ModifiedOnProperty);
		set => SetValue(ModifiedOnProperty, value);
	}

	public ICommand SaveCommand
	{
		get => GetValue(SaveCommandProperty);
		set => SetValue(SaveCommandProperty, value);
	}

	public string Title
	{
		get => GetValue(TitleProperty);
		set => SetValue(TitleProperty, value);
	}

	#endregion
}
