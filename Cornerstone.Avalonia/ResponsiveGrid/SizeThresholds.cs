#region References

using System.ComponentModel;
using Avalonia;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Avalonia.ResponsiveGrid;

[DoNotNotify]
[TypeConverter(typeof(SizeThresholdsTypeConverter))]
public class SizeThresholds : AvaloniaObject
{
	#region Fields

	/// <summary>
	/// Using a AvaloniaProperty as the backing store for Medium to Large.
	/// </summary>
	public static readonly AvaloniaProperty<double> MediumToLargeProperty;

	/// <summary>
	/// Using a AvaloniaProperty as the backing store for Small to Medium.
	/// </summary>
	public static readonly AvaloniaProperty<double> SmallToMediumProperty;

	/// <summary>
	/// Using a AvaloniaProperty as the backing store for XSmall to Small.
	/// </summary>
	public static readonly AvaloniaProperty<double> XSmallToSmallProperty;

	#endregion

	#region Constructors

	static SizeThresholds()
	{
		MediumToLargeProperty = AvaloniaProperty.Register<SizeThresholds, double>(nameof(MediumToLarge), 1200.0);
		SmallToMediumProperty = AvaloniaProperty.Register<SizeThresholds, double>(nameof(SmallToMedium), 992.0);
		XSmallToSmallProperty = AvaloniaProperty.Register<SizeThresholds, double>(nameof(XSmallToSmall), 768.0);
	}

	#endregion

	#region Properties

	public double MediumToLarge
	{
		get => (double) GetValue(MediumToLargeProperty);
		set => SetValue(MediumToLargeProperty, value);
	}

	public double SmallToMedium
	{
		get => (double) GetValue(SmallToMediumProperty);
		set => SetValue(SmallToMediumProperty, value);
	}

	public double XSmallToSmall
	{
		get => (double) GetValue(XSmallToSmallProperty);
		set => SetValue(XSmallToSmallProperty, value);
	}

	#endregion
}