#region References

using Avalonia;
using Avalonia.Controls;

#endregion

namespace Cornerstone.Avalonia.ResponsiveGrid;

public partial class ResponsiveGrid
{
	#region Fields

	public static readonly AvaloniaProperty<int> ActualColumnProperty;
	public static readonly AvaloniaProperty<int> ActualRowProperty;
	public static readonly AvaloniaProperty<int> LG_OffsetProperty;
	public static readonly AvaloniaProperty<int> LGProperty;
	public static readonly AvaloniaProperty<int> LG_PullProperty;
	public static readonly AvaloniaProperty<int> LG_PushProperty;
	public static readonly StyledProperty<int> MaxDivisionProperty;
	public static readonly AvaloniaProperty<int> MD_OffsetProperty;
	public static readonly AvaloniaProperty<int> MDProperty;
	public static readonly AvaloniaProperty<int> MD_PullProperty;
	public static readonly AvaloniaProperty<int> MD_PushProperty;
	public static readonly AvaloniaProperty<int> SM_OffsetProperty;
	public static readonly AvaloniaProperty<int> SMProperty;
	public static readonly AvaloniaProperty<int> SM_PullProperty;
	public static readonly AvaloniaProperty<int> SM_PushProperty;
	public static readonly StyledProperty<SizeThresholds> ThresholdsProperty;
	public static readonly AvaloniaProperty<int> XS_OffsetProperty;
	public static readonly AvaloniaProperty<int> XSProperty;
	public static readonly AvaloniaProperty<int> XS_PullProperty;
	public static readonly AvaloniaProperty<int> XS_PushProperty;

	#endregion

	#region Constructors

	static ResponsiveGrid()
	{
		ActualColumnProperty = AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("ActualColumn");
		ActualRowProperty = AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("ActualRow");
		LG_OffsetProperty = AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("LG_Offset");
		LG_PullProperty = AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("LG_Pull");
		LG_PushProperty = AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("LG_Push");
		LGProperty = AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("Large");
		MaxDivisionProperty = AvaloniaProperty.Register<ResponsiveGrid, int>(nameof(MaxDivision), 12);
		MD_OffsetProperty = AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("SM_Offset");
		MD_PullProperty = AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("MD_Pull");
		MD_PushProperty = AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("MD_Push");
		MDProperty = AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("MD");
		SM_OffsetProperty = AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("SM_Offset");
		SM_PullProperty = AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("SM_Pull");
		SM_PushProperty = AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("SM_Push");
		SMProperty = AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("SM");
		ThresholdsProperty = AvaloniaProperty.Register<ResponsiveGrid, SizeThresholds>(nameof(Thresholds));
		XS_OffsetProperty = AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("XS_Offset");
		XS_PullProperty = AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("XS_Pull");
		XS_PushProperty = AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("XS_Push");
		XSProperty = AvaloniaProperty.RegisterAttached<ResponsiveGrid, Control, int>("XS");

		AffectsMeasure<ResponsiveGrid>(MaxDivisionProperty, ThresholdsProperty,
			XSProperty, SMProperty, MDProperty, LGProperty,
			XS_OffsetProperty, XS_PullProperty, XS_PushProperty,
			SM_OffsetProperty, SM_PullProperty, SM_PushProperty,
			MD_OffsetProperty, MD_PushProperty, MD_PullProperty,
			LG_OffsetProperty, LG_PullProperty, LG_PushProperty
		);
	}

	#endregion

	#region Properties

	public int MaxDivision
	{
		get => GetValue(MaxDivisionProperty);
		set => SetValue(MaxDivisionProperty, value);
	}

	public SizeThresholds Thresholds
	{
		get => GetValue(ThresholdsProperty);
		set => SetValue(ThresholdsProperty, value);
	}

	#endregion

	#region Methods

	public static int GetActualColumn(AvaloniaObject obj)
	{
		return (int) obj.GetValue(ActualColumnProperty);
	}

	public static int GetActualRow(AvaloniaObject obj)
	{
		return (int) obj.GetValue(ActualRowProperty);
	}

	public static int GetLG(AvaloniaObject obj)
	{
		return (int) obj.GetValue(LGProperty);
	}

	public static int GetLG_Offset(AvaloniaObject obj)
	{
		return (int) obj.GetValue(LG_OffsetProperty);
	}

	public static int GetLG_Pull(AvaloniaObject obj)
	{
		return (int) obj.GetValue(LG_PullProperty);
	}

	public static int GetLG_Push(AvaloniaObject obj)
	{
		return (int) obj.GetValue(LG_PushProperty);
	}

	public static int GetMD(AvaloniaObject obj)
	{
		return (int) obj.GetValue(MDProperty);
	}

	public static int GetMD_Offset(AvaloniaObject obj)
	{
		return (int) obj.GetValue(MD_OffsetProperty);
	}

	public static int GetMD_Pull(AvaloniaObject obj)
	{
		return (int) obj.GetValue(MD_PullProperty);
	}

	public static int GetMD_Push(AvaloniaObject obj)
	{
		return (int) obj.GetValue(MD_PushProperty);
	}

	public static int GetSM(AvaloniaObject obj)
	{
		return (int) obj.GetValue(SMProperty);
	}

	public static int GetSM_Offset(AvaloniaObject obj)
	{
		return (int) obj.GetValue(SM_OffsetProperty);
	}

	public static int GetSM_Pull(AvaloniaObject obj)
	{
		return (int) obj.GetValue(SM_PullProperty);
	}

	public static int GetSM_Push(AvaloniaObject obj)
	{
		return (int) obj.GetValue(SM_PushProperty);
	}

	public static int GetXS(AvaloniaObject obj)
	{
		return (int) obj.GetValue(XSProperty);
	}

	public static int GetXS_Offset(AvaloniaObject obj)
	{
		return (int) obj.GetValue(XS_OffsetProperty);
	}

	public static int GetXS_Pull(AvaloniaObject obj)
	{
		return (int) obj.GetValue(XS_PullProperty);
	}

	public static int GetXS_Push(AvaloniaObject obj)
	{
		return (int) obj.GetValue(XS_PushProperty);
	}

	public static void SetLG(AvaloniaObject obj, int value)
	{
		obj.SetValue(LGProperty, value);
	}

	public static void SetLG_Offset(AvaloniaObject obj, int value)
	{
		obj.SetValue(LG_OffsetProperty, value);
	}

	public static void SetLG_Pull(AvaloniaObject obj, int value)
	{
		obj.SetValue(LG_PullProperty, value);
	}

	public static void SetLG_Push(AvaloniaObject obj, int value)
	{
		obj.SetValue(LG_PushProperty, value);
	}

	public static void SetMD(AvaloniaObject obj, int value)
	{
		obj.SetValue(MDProperty, value);
	}

	public static void SetMD_Offset(AvaloniaObject obj, int value)
	{
		obj.SetValue(MD_OffsetProperty, value);
	}

	public static void SetMD_Pull(AvaloniaObject obj, int value)
	{
		obj.SetValue(MD_PullProperty, value);
	}

	public static void SetMD_Push(AvaloniaObject obj, int value)
	{
		obj.SetValue(MD_PushProperty, value);
	}

	public static void SetSM(AvaloniaObject obj, int value)
	{
		obj.SetValue(SMProperty, value);
	}

	public static void SetSM_Offset(AvaloniaObject obj, int value)
	{
		obj.SetValue(SM_OffsetProperty, value);
	}

	public static void SetSM_Pull(AvaloniaObject obj, int value)
	{
		obj.SetValue(SM_PullProperty, value);
	}

	public static void SetSM_Push(AvaloniaObject obj, int value)
	{
		obj.SetValue(SM_PushProperty, value);
	}

	public static void SetXS(AvaloniaObject obj, int value)
	{
		obj.SetValue(XSProperty, value);
	}

	public static void SetXS_Offset(AvaloniaObject obj, int value)
	{
		obj.SetValue(XS_OffsetProperty, value);
	}

	public static void SetXS_Pull(AvaloniaObject obj, int value)
	{
		obj.SetValue(XS_PullProperty, value);
	}

	public static void SetXS_Push(AvaloniaObject obj, int value)
	{
		obj.SetValue(XS_PushProperty, value);
	}

	protected static void SetActualColumn(AvaloniaObject obj, int value)
	{
		obj.SetValue(ActualColumnProperty, value);
	}

	protected static void SetActualRow(AvaloniaObject obj, int value)
	{
		obj.SetValue(ActualRowProperty, value);
	}

	#endregion
}