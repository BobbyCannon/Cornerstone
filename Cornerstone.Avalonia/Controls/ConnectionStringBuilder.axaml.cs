#region References

using Avalonia;
using Cornerstone.Collections;
using Cornerstone.Presentation;
using Cornerstone.Storage;

#endregion

namespace Cornerstone.Avalonia.Controls;

public sealed class ConnectionStringBuilder : CornerstoneContentControl
{
	#region Fields

	public static readonly StyledProperty<DatabaseType> DatabaseTypeProperty;
	public static readonly StyledProperty<bool> IsBuilderOpenProperty;
	public static readonly StyledProperty<bool> IsLightDismissEnabledProperty;
	public static readonly StyledProperty<double> MaxBuilderHeightProperty;

	#endregion

	#region Constructors

	static ConnectionStringBuilder()
	{
		DatabaseTypeProperty = AvaloniaProperty.Register<ConnectionStringBuilder, DatabaseType>(nameof(DatabaseType), DatabaseType.Sql);
		IsBuilderOpenProperty = AvaloniaProperty.Register<ConnectionStringBuilder, bool>(nameof(IsBuilderOpen));
		IsLightDismissEnabledProperty = AvaloniaProperty.Register<ConnectionStringBuilder, bool>(nameof(IsLightDismissEnabled), true);
		MaxBuilderHeightProperty = AvaloniaProperty.Register<ConnectionStringBuilder, double>(nameof(MaxBuilderHeight), 200);
		DatabaseTypes = SelectionOption.GetEnumOptions(DatabaseType.Unknown);
	}

	#endregion

	#region Properties

	public DatabaseType DatabaseType
	{
		get => GetValue(DatabaseTypeProperty);
		set => SetValue(DatabaseTypeProperty, value);
	}

	public static SpeedyList<SelectionOption<DatabaseType>> DatabaseTypes { get; }

	public bool IsBuilderOpen
	{
		get => GetValue(IsBuilderOpenProperty);
		set => SetValue(IsBuilderOpenProperty, value);
	}

	public bool IsLightDismissEnabled
	{
		get => GetValue(IsLightDismissEnabledProperty);
		set => SetValue(IsLightDismissEnabledProperty, value);
	}

	public double MaxBuilderHeight
	{
		get => GetValue(MaxBuilderHeightProperty);
		set => SetValue(MaxBuilderHeightProperty, value);
	}

	#endregion
}