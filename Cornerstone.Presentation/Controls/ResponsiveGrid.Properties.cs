#region References

using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Layout;

#endregion

namespace Cornerstone.Presentation.Controls;

public partial class ResponsiveGrid
{
	#region Fields

	public static readonly PresentationProperty<int> ActualColumnProperty;
	public static readonly PresentationProperty<int> ActualRowProperty;
	public static readonly PresentationProperty<int> LG_OffsetProperty;
	public static readonly PresentationProperty<int> LG_PullProperty;
	public static readonly PresentationProperty<int> LG_PushProperty;
	public static readonly PresentationProperty<int> LGProperty;
	public static readonly StyledProperty<int> MaxDivisionProperty;
	public static readonly PresentationProperty<int> MD_OffsetProperty;
	public static readonly PresentationProperty<int> MD_PullProperty;
	public static readonly PresentationProperty<int> MD_PushProperty;
	public static readonly PresentationProperty<int> MDProperty;
	public static readonly PresentationProperty<int> SM_OffsetProperty;
	public static readonly PresentationProperty<int> SM_PullProperty;
	public static readonly PresentationProperty<int> SM_PushProperty;
	public static readonly PresentationProperty<int> SMProperty;
	public static readonly StyledProperty<SizeThresholds> ThresholdsProperty;
	public static readonly PresentationProperty<int> XS_OffsetProperty;
	public static readonly PresentationProperty<int> XS_PullProperty;
	public static readonly PresentationProperty<int> XS_PushProperty;
	public static readonly PresentationProperty<int> XSProperty;

	#endregion

	#region Constructors

	static ResponsiveGrid()
	{
		ActualColumnProperty = PresentationProperty.RegisterAttached<ResponsiveGrid, Control, int>("ActualColumn");
		ActualRowProperty = PresentationProperty.RegisterAttached<ResponsiveGrid, Control, int>("ActualRow");
		LG_OffsetProperty = PresentationProperty.RegisterAttached<ResponsiveGrid, Control, int>("LG_Offset");
		LG_PullProperty = PresentationProperty.RegisterAttached<ResponsiveGrid, Control, int>("LG_Pull");
		LG_PushProperty = PresentationProperty.RegisterAttached<ResponsiveGrid, Control, int>("LG_Push");
		LGProperty = PresentationProperty.RegisterAttached<ResponsiveGrid, Control, int>("LG");
		MaxDivisionProperty = PresentationProperty.Register<ResponsiveGrid, int>(nameof(MaxDivision), 24);
		MD_OffsetProperty = PresentationProperty.RegisterAttached<ResponsiveGrid, Control, int>("MD_Offset");
		MD_PullProperty = PresentationProperty.RegisterAttached<ResponsiveGrid, Control, int>("MD_Pull");
		MD_PushProperty = PresentationProperty.RegisterAttached<ResponsiveGrid, Control, int>("MD_Push");
		MDProperty = PresentationProperty.RegisterAttached<ResponsiveGrid, Control, int>("MD");
		SM_OffsetProperty = PresentationProperty.RegisterAttached<ResponsiveGrid, Control, int>("SM_Offset");
		SM_PullProperty = PresentationProperty.RegisterAttached<ResponsiveGrid, Control, int>("SM_Pull");
		SM_PushProperty = PresentationProperty.RegisterAttached<ResponsiveGrid, Control, int>("SM_Push");
		SMProperty = PresentationProperty.RegisterAttached<ResponsiveGrid, Control, int>("SM");
		ThresholdsProperty = PresentationProperty.Register<ResponsiveGrid, SizeThresholds>(nameof(Thresholds));
		XS_OffsetProperty = PresentationProperty.RegisterAttached<ResponsiveGrid, Control, int>("XS_Offset");
		XS_PullProperty = PresentationProperty.RegisterAttached<ResponsiveGrid, Control, int>("XS_Pull");
		XS_PushProperty = PresentationProperty.RegisterAttached<ResponsiveGrid, Control, int>("XS_Push");
		XSProperty = PresentationProperty.RegisterAttached<ResponsiveGrid, Control, int>("XS");

		AffectsMeasure<ResponsiveGrid>(MaxDivisionProperty, ThresholdsProperty, ColumnSpacingProperty, RowSpacingProperty);

		XSProperty.Changed.AddClassHandler<Control>(OnChildLayoutPropertyChanged);
		SMProperty.Changed.AddClassHandler<Control>(OnChildLayoutPropertyChanged);
		MDProperty.Changed.AddClassHandler<Control>(OnChildLayoutPropertyChanged);
		LGProperty.Changed.AddClassHandler<Control>(OnChildLayoutPropertyChanged);
		XS_OffsetProperty.Changed.AddClassHandler<Control>(OnChildLayoutPropertyChanged);
		SM_OffsetProperty.Changed.AddClassHandler<Control>(OnChildLayoutPropertyChanged);
		MD_OffsetProperty.Changed.AddClassHandler<Control>(OnChildLayoutPropertyChanged);
		LG_OffsetProperty.Changed.AddClassHandler<Control>(OnChildLayoutPropertyChanged);
		XS_PullProperty.Changed.AddClassHandler<Control>(OnChildLayoutPropertyChanged);
		SM_PullProperty.Changed.AddClassHandler<Control>(OnChildLayoutPropertyChanged);
		MD_PullProperty.Changed.AddClassHandler<Control>(OnChildLayoutPropertyChanged);
		LG_PullProperty.Changed.AddClassHandler<Control>(OnChildLayoutPropertyChanged);
		XS_PushProperty.Changed.AddClassHandler<Control>(OnChildLayoutPropertyChanged);
		SM_PushProperty.Changed.AddClassHandler<Control>(OnChildLayoutPropertyChanged);
		MD_PushProperty.Changed.AddClassHandler<Control>(OnChildLayoutPropertyChanged);
		LG_PushProperty.Changed.AddClassHandler<Control>(OnChildLayoutPropertyChanged);
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
		get
		{
			var value = GetValue(ThresholdsProperty);
			if (value == null)
			{
				value = new SizeThresholds();
				SetValue(ThresholdsProperty, value);
			}

			return value;
		}
		set => SetValue(ThresholdsProperty, value ?? new SizeThresholds());
	}

	#endregion

	#region Methods

	public static int GetActualColumn(PresentationObject obj)
	{
		return (int) obj.GetValue(ActualColumnProperty);
	}

	public static int GetActualRow(PresentationObject obj)
	{
		return (int) obj.GetValue(ActualRowProperty);
	}

	public static int GetLG(PresentationObject obj)
	{
		return (int) obj.GetValue(LGProperty);
	}

	public static int GetLG_Offset(PresentationObject obj)
	{
		return (int) obj.GetValue(LG_OffsetProperty);
	}

	public static int GetLG_Pull(PresentationObject obj)
	{
		return (int) obj.GetValue(LG_PullProperty);
	}

	public static int GetLG_Push(PresentationObject obj)
	{
		return (int) obj.GetValue(LG_PushProperty);
	}

	public static int GetMD(PresentationObject obj)
	{
		return (int) obj.GetValue(MDProperty);
	}

	public static int GetMD_Offset(PresentationObject obj)
	{
		return (int) obj.GetValue(MD_OffsetProperty);
	}

	public static int GetMD_Pull(PresentationObject obj)
	{
		return (int) obj.GetValue(MD_PullProperty);
	}

	public static int GetMD_Push(PresentationObject obj)
	{
		return (int) obj.GetValue(MD_PushProperty);
	}

	public static int GetSM(PresentationObject obj)
	{
		return (int) obj.GetValue(SMProperty);
	}

	public static int GetSM_Offset(PresentationObject obj)
	{
		return (int) obj.GetValue(SM_OffsetProperty);
	}

	public static int GetSM_Pull(PresentationObject obj)
	{
		return (int) obj.GetValue(SM_PullProperty);
	}

	public static int GetSM_Push(PresentationObject obj)
	{
		return (int) obj.GetValue(SM_PushProperty);
	}

	public static int GetXS(PresentationObject obj)
	{
		return (int) obj.GetValue(XSProperty);
	}

	public static int GetXS_Offset(PresentationObject obj)
	{
		return (int) obj.GetValue(XS_OffsetProperty);
	}

	public static int GetXS_Pull(PresentationObject obj)
	{
		return (int) obj.GetValue(XS_PullProperty);
	}

	public static int GetXS_Push(PresentationObject obj)
	{
		return (int) obj.GetValue(XS_PushProperty);
	}

	public static void SetLG(PresentationObject obj, int value)
	{
		obj.SetValue(LGProperty, value);
	}

	public static void SetLG_Offset(PresentationObject obj, int value)
	{
		obj.SetValue(LG_OffsetProperty, value);
	}

	public static void SetLG_Pull(PresentationObject obj, int value)
	{
		obj.SetValue(LG_PullProperty, value);
	}

	public static void SetLG_Push(PresentationObject obj, int value)
	{
		obj.SetValue(LG_PushProperty, value);
	}

	public static void SetMD(PresentationObject obj, int value)
	{
		obj.SetValue(MDProperty, value);
	}

	public static void SetMD_Offset(PresentationObject obj, int value)
	{
		obj.SetValue(MD_OffsetProperty, value);
	}

	public static void SetMD_Pull(PresentationObject obj, int value)
	{
		obj.SetValue(MD_PullProperty, value);
	}

	public static void SetMD_Push(PresentationObject obj, int value)
	{
		obj.SetValue(MD_PushProperty, value);
	}

	public static void SetSM(PresentationObject obj, int value)
	{
		obj.SetValue(SMProperty, value);
	}

	public static void SetSM_Offset(PresentationObject obj, int value)
	{
		obj.SetValue(SM_OffsetProperty, value);
	}

	public static void SetSM_Pull(PresentationObject obj, int value)
	{
		obj.SetValue(SM_PullProperty, value);
	}

	public static void SetSM_Push(PresentationObject obj, int value)
	{
		obj.SetValue(SM_PushProperty, value);
	}

	public static void SetXS(PresentationObject obj, int value)
	{
		obj.SetValue(XSProperty, value);
	}

	public static void SetXS_Offset(PresentationObject obj, int value)
	{
		obj.SetValue(XS_OffsetProperty, value);
	}

	public static void SetXS_Pull(PresentationObject obj, int value)
	{
		obj.SetValue(XS_PullProperty, value);
	}

	public static void SetXS_Push(PresentationObject obj, int value)
	{
		obj.SetValue(XS_PushProperty, value);
	}

	private static void OnChildLayoutPropertyChanged(Control control, PresentationPropertyChangedEventArgs args)
	{
		if (control.Parent is ResponsiveGrid grid)
		{
			grid.InvalidateMeasure();
		}
	}

	protected static void SetActualColumn(PresentationObject obj, int value)
	{
		obj.SetValue(ActualColumnProperty, value);
	}

	protected static void SetActualRow(PresentationObject obj, int value)
	{
		obj.SetValue(ActualRowProperty, value);
	}

	#endregion
}