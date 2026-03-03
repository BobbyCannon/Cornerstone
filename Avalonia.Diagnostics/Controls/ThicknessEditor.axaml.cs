#region References

using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;

#endregion

namespace Avalonia.Diagnostics.Controls;

public class ThicknessEditor : ContentControl
{
	#region Fields

	public static readonly StyledProperty<double> BottomProperty;
	public static readonly StyledProperty<string> HeaderProperty;
	public static readonly StyledProperty<IBrush> HighlightProperty;
	public static readonly StyledProperty<bool> IsPresentProperty;
	public static readonly StyledProperty<double> LeftProperty;
	public static readonly StyledProperty<double> RightProperty;
	public static readonly StyledProperty<Thickness> ThicknessProperty;
	public static readonly StyledProperty<double> TopProperty;

	private bool _isUpdatingThickness;

	#endregion

	#region Constructors

	static ThicknessEditor()
	{
		BottomProperty = AvaloniaProperty.Register<ThicknessEditor, double>(nameof(Bottom));
		HeaderProperty = AvaloniaProperty.Register<ThicknessEditor, string>(nameof(Header));
		HighlightProperty = AvaloniaProperty.Register<ThicknessEditor, IBrush>(nameof(Highlight));
		IsPresentProperty = AvaloniaProperty.Register<ThicknessEditor, bool>(nameof(IsPresent), true);
		LeftProperty = AvaloniaProperty.Register<ThicknessEditor, double>(nameof(Left));
		RightProperty = AvaloniaProperty.Register<ThicknessEditor, double>(nameof(Right));
		ThicknessProperty = AvaloniaProperty.Register<ThicknessEditor, Thickness>(nameof(Thickness),
			defaultBindingMode: BindingMode.TwoWay);
		TopProperty = AvaloniaProperty.Register<ThicknessEditor, double>(nameof(Top));
	}

	#endregion

	#region Properties

	public double Bottom
	{
		get => GetValue(BottomProperty);
		set => SetValue(BottomProperty, value);
	}

	public string Header
	{
		get => GetValue(HeaderProperty);
		set => SetValue(HeaderProperty, value);
	}

	public IBrush Highlight
	{
		get => GetValue(HighlightProperty);
		set => SetValue(HighlightProperty, value);
	}

	public bool IsPresent
	{
		get => GetValue(IsPresentProperty);
		set => SetValue(IsPresentProperty, value);
	}

	public double Left
	{
		get => GetValue(LeftProperty);
		set => SetValue(LeftProperty, value);
	}

	public double Right
	{
		get => GetValue(RightProperty);
		set => SetValue(RightProperty, value);
	}

	public Thickness Thickness
	{
		get => GetValue(ThicknessProperty);
		set => SetValue(ThicknessProperty, value);
	}

	public double Top
	{
		get => GetValue(TopProperty);
		set => SetValue(TopProperty, value);
	}

	#endregion

	#region Methods

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (change.Property == ThicknessProperty)
		{
			try
			{
				_isUpdatingThickness = true;

				var value = change.GetNewValue<Thickness>();

				SetCurrentValue(LeftProperty, value.Left);
				SetCurrentValue(TopProperty, value.Top);
				SetCurrentValue(RightProperty, value.Right);
				SetCurrentValue(BottomProperty, value.Bottom);
			}
			finally
			{
				_isUpdatingThickness = false;
			}
		}
		else if (!_isUpdatingThickness &&
				((change.Property == LeftProperty) || (change.Property == TopProperty) ||
					(change.Property == RightProperty) || (change.Property == BottomProperty)))
		{
			SetCurrentValue(ThicknessProperty, new(Left, Top, Right, Bottom));
		}
	}

	#endregion
}