#region References

using System.ComponentModel;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;

#endregion

namespace Cornerstone.Presentation.Controls.Layout;

[TypeConverter(typeof(SizeThresholdsTypeConverter))]
public class SizeThresholds : PresentationObject
{
	#region Fields

	/// <summary>
	/// Using a PresentationProperty as the backing store for Medium to Large.
	/// </summary>
	public static readonly PresentationProperty<double> MediumToLargeProperty;

	/// <summary>
	/// Using a PresentationProperty as the backing store for Small to Medium.
	/// </summary>
	public static readonly PresentationProperty<double> SmallToMediumProperty;

	/// <summary>
	/// Using a PresentationProperty as the backing store for XSmall to Small.
	/// </summary>
	public static readonly PresentationProperty<double> XSmallToSmallProperty;

	#endregion

	#region Constructors

	static SizeThresholds()
	{
		// Large  > 1200
		// Medium > 1000 && <= 1200 
		// Small  >  800 && <= 1000 
		// XSmall <  800
		MediumToLargeProperty = PresentationProperty.Register<SizeThresholds, double>(nameof(MediumToLarge), 1200.0);
		SmallToMediumProperty = PresentationProperty.Register<SizeThresholds, double>(nameof(SmallToMedium), 1000.0);
		XSmallToSmallProperty = PresentationProperty.Register<SizeThresholds, double>(nameof(XSmallToSmall), 800.0);
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