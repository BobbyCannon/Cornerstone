#region References

using System.ComponentModel;
using Cornerstone.Attributes;
using Cornerstone.Avalonia;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Sample.Tabs;
using Cornerstone.Settings;

#endregion

namespace Cornerstone.Sample.ViewModels;

public class ApplicationSettings : SettingsFile<ApplicationSettings>
{
	#region Constructors

	/// <summary>
	/// For serialization, do not use.
	/// </summary>
	public ApplicationSettings() : this(new RuntimeInformationData())
	{
	}

	[DependencyInjectionConstructor]
	public ApplicationSettings(IRuntimeInformation runtimeInformation)
		: base("ApplicationSettings.json", runtimeInformation, null)
	{
		Set(x => x.MainWindowLocation, new WindowLocation());
	}

	#endregion

	#region Properties

	[Category("Theme")]
	public bool CycleThemes
	{
		get => Get(true);
		set => Set(value);
	}

	public WindowLocation MainWindowLocation => Get<WindowLocation>();

	public string SelectedTabName
	{
		get => Get(TabThemes.HeaderName);
		set => Set(value);
	}

	[Category("Theme")]
	public ThemeColor ThemeColor
	{
		get => Get(ThemeColor.Blue);
		set => Set(value);
	}

	[Category("Theme")]
	public bool UseDarkMode
	{
		get => Get(true);
		set => Set(value);
	}

	[Category("Control")]
	public bool UseGamepadForInput
	{
		get => Get(false);
		set => Set(value);
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override bool HasChanges(IncludeExcludeSettings settings)
	{
		return base.HasChanges(settings)
			|| MainWindowLocation.HasChanges();
	}

	/// <inheritdoc />
	public override void ResetHasChanges()
	{
		MainWindowLocation.ResetHasChanges();
		base.ResetHasChanges();
	}

	/// <inheritdoc />
	protected override void OnPropertyChanged(string propertyName = null)
	{
		switch (propertyName)
		{
			case nameof(ThemeColor):
			{
				Theme.SetThemeColor(ThemeColor);
				break;
			}
			case nameof(UseDarkMode):
			{
				Theme.SetThemeVariant(UseDarkMode);
				break;
			}
		}

		base.OnPropertyChanged(propertyName);
	}

	#endregion
}