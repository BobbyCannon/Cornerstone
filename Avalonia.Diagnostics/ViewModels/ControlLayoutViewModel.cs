#region References

using System;
using System.ComponentModel;
using System.Text;
using Avalonia.Controls;
using Avalonia.Layout;
using Cornerstone.Presentation;

#endregion

namespace Avalonia.Diagnostics.ViewModels;

public class ControlLayoutViewModel : ViewModel
{
	#region Fields

	private Thickness _borderThickness;
	private readonly Visual _control;
	private double _height;
	private string _heightConstraint;
	private HorizontalAlignment _horizontalAlignment;
	private Thickness _marginThickness;
	private Thickness _paddingThickness;
	private bool _updatingFromControl;
	private VerticalAlignment _verticalAlignment;
	private double _width;
	private string _widthConstraint;

	#endregion

	#region Constructors

	public ControlLayoutViewModel(Visual control)
	{
		_control = control;

		HasPadding = AvaloniaPropertyRegistry.Instance.IsRegistered(control, Decorator.PaddingProperty);
		HasBorder = AvaloniaPropertyRegistry.Instance.IsRegistered(control, Border.BorderThicknessProperty);

		if (control is AvaloniaObject ao)
		{
			try
			{
				_updatingFromControl = true;
				MarginThickness = ao.GetValue(Layoutable.MarginProperty);

				if (HasPadding)
				{
					PaddingThickness = ao.GetValue(Decorator.PaddingProperty);
				}

				if (HasBorder)
				{
					BorderThickness = ao.GetValue(Border.BorderThicknessProperty);
				}

				HorizontalAlignment = ao.GetValue(Layoutable.HorizontalAlignmentProperty);
				VerticalAlignment = ao.GetValue(Layoutable.VerticalAlignmentProperty);
			}
			finally
			{
				_updatingFromControl = false;
			}
		}

		UpdateSize();
		UpdateSizeConstraints();
	}

	#endregion

	#region Properties

	public Thickness BorderThickness
	{
		get => _borderThickness;
		set => SetProperty(ref _borderThickness, value);
	}

	public bool HasBorder { get; }

	public bool HasPadding { get; }

	public double Height
	{
		get => _height;
		private set => SetProperty(ref _height, value);
	}

	public string HeightConstraint
	{
		get => _heightConstraint;
		private set => SetProperty(ref _heightConstraint, value);
	}

	public HorizontalAlignment HorizontalAlignment
	{
		get => _horizontalAlignment;
		set => SetProperty(ref _horizontalAlignment, value);
	}

	public Thickness MarginThickness
	{
		get => _marginThickness;
		set => SetProperty(ref _marginThickness, value);
	}

	public Thickness PaddingThickness
	{
		get => _paddingThickness;
		set => SetProperty(ref _paddingThickness, value);
	}

	public VerticalAlignment VerticalAlignment
	{
		get => _verticalAlignment;
		set => SetProperty(ref _verticalAlignment, value);
	}

	public double Width
	{
		get => _width;
		private set => SetProperty(ref _width, value);
	}

	public string WidthConstraint
	{
		get => _widthConstraint;
		private set => SetProperty(ref _widthConstraint, value);
	}

	#endregion

	#region Methods

	public void ControlPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
	{
		try
		{
			_updatingFromControl = true;

			if (e.Property == Visual.BoundsProperty)
			{
				UpdateSize();
			}
			else
			{
				if (_control is AvaloniaObject ao)
				{
					if (e.Property == Layoutable.MarginProperty)
					{
						MarginThickness = ao.GetValue(Layoutable.MarginProperty);
					}
					else if (e.Property == Decorator.PaddingProperty)
					{
						PaddingThickness = ao.GetValue(Decorator.PaddingProperty);
					}
					else if (e.Property == Border.BorderThicknessProperty)
					{
						BorderThickness = ao.GetValue(Border.BorderThicknessProperty);
					}
					else if ((e.Property == Layoutable.MinWidthProperty) ||
							(e.Property == Layoutable.MaxWidthProperty) ||
							(e.Property == Layoutable.MinHeightProperty) ||
							(e.Property == Layoutable.MaxHeightProperty))
					{
						UpdateSizeConstraints();
					}
					else if (e.Property == Layoutable.HorizontalAlignmentProperty)
					{
						HorizontalAlignment = ao.GetValue(Layoutable.HorizontalAlignmentProperty);
					}
					else if (e.Property == Layoutable.VerticalAlignmentProperty)
					{
						VerticalAlignment = ao.GetValue(Layoutable.VerticalAlignmentProperty);
					}
				}
			}
		}
		finally
		{
			_updatingFromControl = false;
		}
	}

	protected override void OnPropertyChanged(string propertyName = null)
	{
		base.OnPropertyChanged(propertyName);
	
		if (_updatingFromControl)
		{
			return;
		}

		if (_control is AvaloniaObject ao)
		{
			if (propertyName == nameof(MarginThickness))
			{
				ao.SetValue(Layoutable.MarginProperty, MarginThickness);
			}
			else if (HasPadding && (propertyName == nameof(PaddingThickness)))
			{
				ao.SetValue(Decorator.PaddingProperty, PaddingThickness);
			}
			else if (HasBorder && (propertyName == nameof(BorderThickness)))
			{
				ao.SetValue(Border.BorderThicknessProperty, BorderThickness);
			}
			else if (propertyName == nameof(HorizontalAlignment))
			{
				ao.SetValue(Layoutable.HorizontalAlignmentProperty, HorizontalAlignment);
			}
			else if (propertyName == nameof(VerticalAlignment))
			{
				ao.SetValue(Layoutable.VerticalAlignmentProperty, VerticalAlignment);
			}
		}
	}

	private void UpdateSize()
	{
		var size = _control.Bounds;

		Width = Math.Round(size.Width, 2);
		Height = Math.Round(size.Height, 2);
	}

	private void UpdateSizeConstraints()
	{
		if (_control is AvaloniaObject ao)
		{
			string CreateConstraintInfo(StyledProperty<double> minProperty, StyledProperty<double> maxProperty)
			{
				var hasMin = ao.IsSet(minProperty);
				var hasMax = ao.IsSet(maxProperty);

				if (hasMin || hasMax)
				{
					var builder = new StringBuilder();

					if (hasMin)
					{
						var minValue = ao.GetValue(minProperty);
						builder.AppendFormat("Min: {0}", Math.Round(minValue, 2));
						builder.AppendLine();
					}

					if (hasMax)
					{
						var maxValue = ao.GetValue(maxProperty);
						builder.AppendFormat("Max: {0}", Math.Round(maxValue, 2));
					}

					return builder.ToString();
				}

				return null;
			}

			WidthConstraint = CreateConstraintInfo(Layoutable.MinWidthProperty, Layoutable.MaxWidthProperty);
			HeightConstraint = CreateConstraintInfo(Layoutable.MinHeightProperty, Layoutable.MaxHeightProperty);
		}
	}

	#endregion
}