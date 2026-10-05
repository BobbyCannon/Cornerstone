#region References

using System;
using Cornerstone.Presentation.Controls.Documents;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Logging;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.Media;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Resources;
using Cornerstone.Presentation.Controls.Primitives;

#endregion

namespace Cornerstone.Presentation.Controls;

public class TemplatedControl<T> : TemplatedControl
	where T : class
{
	#region Fields

	public static readonly StyledProperty<T> ViewModelProperty =
		PresentationProperty.Register<TemplatedControl<T>, T>(nameof(ViewModel));

	#endregion

	#region Properties

	public T ViewModel
	{
		get => GetValue(ViewModelProperty);
		set => SetValue(ViewModelProperty, value);
	}

	#endregion

	#region Methods

	protected override object GetViewModel()
	{
		return ViewModel;
	}

	protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
	{
		if ((change.Property == DataContextProperty)
			&& DataContext is T viewModel)
		{
			ViewModel = viewModel;
		}

		if (change.Property == ViewModelProperty)
		{
			ViewModelChangedHook?.Invoke(
				this, change.OldValue, change.NewValue, DataContext, VisualRoot != null);
		}

		base.OnPropertyChanged(change);
	}

	#endregion
}

/// <summary>
/// A lookless control whose visual appearance is defined by its <see cref="Template" />.
/// </summary>
public class TemplatedControl : Control
{
	#region Fields

	/// <summary>
	/// Defines the <see cref="Background" /> property.
	/// </summary>
	public static readonly StyledProperty<IBrush?> BackgroundProperty;

	/// <summary>
	/// Defines the <see cref="BackgroundSizing" /> property.
	/// </summary>
	public static readonly StyledProperty<BackgroundSizing> BackgroundSizingProperty;

	/// <summary>
	/// Defines the <see cref="BorderBrush" /> property.
	/// </summary>
	public static readonly StyledProperty<IBrush?> BorderBrushProperty;

	/// <summary>
	/// Defines the <see cref="BorderThickness" /> property.
	/// </summary>
	public static readonly StyledProperty<Thickness> BorderThicknessProperty;

	/// <summary>
	/// Defines the <see cref="CornerRadius" /> property.
	/// </summary>
	public static readonly StyledProperty<CornerRadius> CornerRadiusProperty;

	/// <summary>
	/// Defines the <see cref="FontFamily" /> property.
	/// </summary>
	public static readonly StyledProperty<FontFamily> FontFamilyProperty;

	/// <summary>
	/// Defines the <see cref="FontFeaturesProperty" /> property.
	/// </summary>
	public static readonly StyledProperty<FontFeatureCollection?> FontFeaturesProperty;

	/// <summary>
	/// Defines the <see cref="FontSize" /> property.
	/// </summary>
	public static readonly StyledProperty<double> FontSizeProperty;

	/// <summary>
	/// Defines the <see cref="FontWeight" /> property.
	/// </summary>
	public static readonly StyledProperty<FontStretch> FontStretchProperty;

	/// <summary>
	/// Defines the <see cref="FontStyle" /> property.
	/// </summary>
	public static readonly StyledProperty<FontStyle> FontStyleProperty;

	/// <summary>
	/// Defines the <see cref="FontWeight" /> property.
	/// </summary>
	public static readonly StyledProperty<FontWeight> FontWeightProperty;

	/// <summary>
	/// Defines the <see cref="Foreground" /> property.
	/// </summary>
	public static readonly StyledProperty<IBrush?> ForegroundProperty;

	/// <summary>
	/// Defines the IsTemplateFocusTarget attached property.
	/// </summary>
	public static readonly AttachedProperty<bool> IsTemplateFocusTargetProperty;

	/// <summary>
	/// Defines the <see cref="LetterSpacing" /> property.
	/// </summary>
	public static readonly StyledProperty<double> LetterSpacingProperty;

	/// <summary>
	/// Defines the <see cref="Padding" /> property.
	/// </summary>
	public static readonly StyledProperty<Thickness> PaddingProperty;

	/// <summary>
	/// Defines the <see cref="TemplateApplied" /> routed event.
	/// </summary>
	public static readonly RoutedEvent<TemplateAppliedEventArgs> TemplateAppliedEvent;

	/// <summary>
	/// Defines the <see cref="Template" /> property.
	/// </summary>
	public static readonly StyledProperty<IControlTemplate?> TemplateProperty;

	private IControlTemplate? _appliedTemplate;
	private Typeface? _cachedTypeface;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes static members of the <see cref="TemplatedControl" /> class.
	/// </summary>
	static TemplatedControl()
	{
		BackgroundProperty = Border.BackgroundProperty.AddOwner<TemplatedControl>();
		BackgroundSizingProperty = Border.BackgroundSizingProperty.AddOwner<TemplatedControl>();
		BorderBrushProperty = Border.BorderBrushProperty.AddOwner<TemplatedControl>();
		BorderThicknessProperty = Border.BorderThicknessProperty.AddOwner<TemplatedControl>();
		CornerRadiusProperty = Border.CornerRadiusProperty.AddOwner<TemplatedControl>();
		FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner<TemplatedControl>();
		FontFeaturesProperty = TextElement.FontFeaturesProperty.AddOwner<TemplatedControl>();
		FontSizeProperty = TextElement.FontSizeProperty.AddOwner<TemplatedControl>();
		FontStyleProperty = TextElement.FontStyleProperty.AddOwner<TemplatedControl>();
		FontWeightProperty = TextElement.FontWeightProperty.AddOwner<TemplatedControl>();
		FontStretchProperty = TextElement.FontStretchProperty.AddOwner<TemplatedControl>();
		ForegroundProperty = TextElement.ForegroundProperty.AddOwner<TemplatedControl>();
		LetterSpacingProperty = TextElement.LetterSpacingProperty.AddOwner<TemplatedControl>();
		PaddingProperty = Decorator.PaddingProperty.AddOwner<TemplatedControl>();
		TemplateProperty = PresentationProperty.Register<TemplatedControl, IControlTemplate>(nameof(Template));
		IsTemplateFocusTargetProperty = PresentationProperty.RegisterAttached<TemplatedControl, Control, bool>("IsTemplateFocusTarget");
		TemplateAppliedEvent = RoutedEvent.Register<TemplatedControl, TemplateAppliedEventArgs>(nameof(TemplateApplied), RoutingStrategies.Direct);
		ClipToBoundsProperty.OverrideDefaultValue<TemplatedControl>(true);
		TemplateProperty.Changed.AddClassHandler<TemplatedControl>((x, e) => x.OnTemplateChanged(e));
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets the brush used to draw the control's background.
	/// </summary>
	public IBrush? Background
	{
		get => GetValue(BackgroundProperty);
		set => SetValue(BackgroundProperty, value);
	}

	/// <summary>
	/// Gets or sets how the control's background is drawn relative to the control's border.
	/// </summary>
	public BackgroundSizing BackgroundSizing
	{
		get => GetValue(BackgroundSizingProperty);
		set => SetValue(BackgroundSizingProperty, value);
	}

	/// <summary>
	/// Gets or sets the brush used to draw the control's border.
	/// </summary>
	public IBrush? BorderBrush
	{
		get => GetValue(BorderBrushProperty);
		set => SetValue(BorderBrushProperty, value);
	}

	/// <summary>
	/// Gets or sets the thickness of the control's border.
	/// </summary>
	public Thickness BorderThickness
	{
		get => GetValue(BorderThicknessProperty);
		set => SetValue(BorderThicknessProperty, value);
	}

	/// <summary>
	/// Gets or sets the radius of the border rounded corners.
	/// </summary>
	public CornerRadius CornerRadius
	{
		get => GetValue(CornerRadiusProperty);
		set => SetValue(CornerRadiusProperty, value);
	}

	/// <summary>
	/// Gets or sets the font family used to draw the control's text.
	/// </summary>
	public FontFamily FontFamily
	{
		get => GetValue(FontFamilyProperty);
		set => SetValue(FontFamilyProperty, value);
	}

	/// <summary>
	/// Gets or sets the font features turned on/off.
	/// </summary>
	public FontFeatureCollection? FontFeatures
	{
		get => GetValue(FontFeaturesProperty);
		set => SetValue(FontFeaturesProperty, value);
	}

	/// <summary>
	/// Gets or sets the size of the control's text in points.
	/// </summary>
	public double FontSize
	{
		get => GetValue(FontSizeProperty);
		set => SetValue(FontSizeProperty, value);
	}

	/// <summary>
	/// Gets or sets the font stretch used to draw the control's text.
	/// </summary>
	public FontStretch FontStretch
	{
		get => GetValue(FontStretchProperty);
		set => SetValue(FontStretchProperty, value);
	}

	/// <summary>
	/// Gets or sets the font style used to draw the control's text.
	/// </summary>
	public FontStyle FontStyle
	{
		get => GetValue(FontStyleProperty);
		set => SetValue(FontStyleProperty, value);
	}

	/// <summary>
	/// Gets or sets the font weight used to draw the control's text.
	/// </summary>
	public FontWeight FontWeight
	{
		get => GetValue(FontWeightProperty);
		set => SetValue(FontWeightProperty, value);
	}

	/// <summary>
	/// Gets or sets the brush used to draw the control's text and other foreground elements.
	/// </summary>
	public IBrush? Foreground
	{
		get => GetValue(ForegroundProperty);
		set => SetValue(ForegroundProperty, value);
	}

	/// <summary>
	/// Gets or sets the letter spacing for the control's text content.
	/// </summary>
	public double LetterSpacing
	{
		get => GetValue(LetterSpacingProperty);
		set => SetValue(LetterSpacingProperty, value);
	}

	/// <summary>
	/// Gets or sets the padding placed between the border of the control and its content.
	/// </summary>
	public Thickness Padding
	{
		get => GetValue(PaddingProperty);
		set => SetValue(PaddingProperty, value);
	}

	/// <summary>
	/// Gets or sets the template that defines the control's appearance.
	/// </summary>
	public IControlTemplate? Template
	{
		get => GetValue(TemplateProperty);
		set => SetValue(TemplateProperty, value);
	}

	/// <summary>
	/// Cached typeface built from the current font family, style, weight, and stretch.
	/// Cleared when those properties change.
	/// </summary>
	public Typeface Typeface => _cachedTypeface ??= new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);

	#endregion

	#region Methods

	public static T GetInstance<T>()
	{
		return AppBootstrap.GetInstance<T>();
	}

	public static object GetInstance(Type type)
	{
		return AppBootstrap.GetInstance(type);
	}

	/// <inheritdoc />
	public sealed override void ApplyTemplate()
	{
		var template = Template;

		// Apply the template if it is not the same as the template already applied - except
		// for in the case that the template is null, and we're not attached to the logical 
		// tree. In that case, the template has probably been cleared because the style setting
		// the template has been detached, so we want to wait until it's re-attached to the 
		// logical tree as if it's re-attached to the same tree the template will be the same,
		// and we don't need to do anything.
		if ((_appliedTemplate != template) && ((template != null) || ((ILogical) this).IsAttachedToLogicalTree))
		{
			if (VisualChildren.Count > 0)
			{
				foreach (var child in this.GetTemplateDescendants())
				{
					child.TemplatedParent = null;
					((ISetLogicalParent) child).SetParent(null);
				}

				VisualChildren.Clear();
			}

			if (template != null)
			{
				Logger.TryGet(LogEventLevel.Verbose, LogArea.Control)?.Log(this, "Creating control template");

				if (template.Build(this) is { } templateResult)
				{
					var (child, nameScope) = templateResult;
					ApplyTemplatedParent(child, this);

					((ISetLogicalParent) child).SetParent(this);
					VisualChildren.Add(child);

					var e = new TemplateAppliedEventArgs(nameScope);
					using (AppBootstrap.StartupProfiler.Accumulate("OnApply." + GetType().Name))
					{
						OnApplyTemplate(e);
					}

					using (AppBootstrap.StartupProfiler.Accumulate("Template.RaiseTemplateApplied"))
					{
						RaiseEvent(e);
					}
				}
			}

			_appliedTemplate = template;
		}
	}

	/// <summary>
	/// Gets the value of the IsTemplateFocusTargetProperty attached property on a control.
	/// </summary>
	/// <param name="control"> The control. </param>
	/// <returns> The property value. </returns>
	/// <see cref="SetIsTemplateFocusTarget(Control, bool)" />
	public static bool GetIsTemplateFocusTarget(Control control)
	{
		return control.GetValue(IsTemplateFocusTargetProperty);
	}

	/// <summary>
	/// Sets the value of the IsTemplateFocusTargetProperty attached property on a control.
	/// </summary>
	/// <param name="control"> The control. </param>
	/// <param name="value"> The property value. </param>
	/// <remarks>
	/// When a control is navigated to using the keyboard, a focus adorner is shown - usually
	/// around the control itself. However, if the TemplatedControl.IsTemplateFocusTarget
	/// attached property is set to true on an element in the control template, then the focus
	/// adorner will be shown around that control instead.
	/// </remarks>
	public static void SetIsTemplateFocusTarget(Control control, bool value)
	{
		control.SetValue(IsTemplateFocusTargetProperty, value);
	}

	/// <inheritdoc />
	protected override Control GetTemplateFocusTarget()
	{
		foreach (var child in this.GetTemplateDescendants())
		{
			if (child is Control control && GetIsTemplateFocusTarget(control))
			{
				return control;
			}
		}

		return this;
	}

	/// <summary>
	/// Called when the control's template is applied.
	/// In simple terms, this means the method is called just before the control is displayed.
	/// </summary>
	/// <param name="e"> The event args. </param>
	protected virtual void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
	}

	protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if ((change.Property == FontFamilyProperty)
			|| (change.Property == FontSizeProperty)
			|| (change.Property == FontStretchProperty)
			|| (change.Property == FontStyleProperty)
			|| (change.Property == FontWeightProperty)
			|| (change.Property == ForegroundProperty))
		{
			_cachedTypeface = null;
			InvalidateVisual();
		}
	}

	/// <inheritdoc />
	protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
	{
		if (VisualChildren.Count > 0)
		{
			((ILogical) VisualChildren[0]).NotifyAttachedToLogicalTree(e);
		}

		base.OnAttachedToLogicalTree(e);
	}

	/// <inheritdoc />
	protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
	{
		if (VisualChildren.Count > 0)
		{
			((ILogical) VisualChildren[0]).NotifyDetachedFromLogicalTree(e);
		}

		base.OnDetachedFromLogicalTree(e);
	}

	/// <summary>
	/// Called when the <see cref="Template" /> property changes.
	/// </summary>
	/// <param name="e"> The event args. </param>
	protected virtual void OnTemplateChanged(PresentationPropertyChangedEventArgs e)
	{
		InvalidateMeasure();
	}

	/// <summary>
	/// Sets the TemplatedParent property for the created template children.
	/// </summary>
	/// <param name="control"> The control. </param>
	/// <param name="templatedParent"> The templated parent to apply. </param>
	internal static void ApplyTemplatedParent(StyledElement control, PresentationObject? templatedParent)
	{
		using (AppBootstrap.StartupProfiler.Accumulate("Template.TemplatedParent"))
		{
			control.TemplatedParent = templatedParent;
		}

		var children = control.LogicalChildren;
		var count = children.Count;

		for (var i = 0; i < count; i++)
		{
			if (children[i] is StyledElement child && child.TemplatedParent is null)
			{
				ApplyTemplatedParent(child, templatedParent);
			}
		}
	}

	/// <inheritdoc />
	internal sealed override void NotifyChildResourcesChanged(ResourcesChangedEventArgs e)
	{
		var count = VisualChildren.Count;

		for (var i = 0; i < count; ++i)
		{
			if (VisualChildren[i] is ILogical logical)
			{
				logical.NotifyResourcesChanged(e);
			}
		}

		base.NotifyChildResourcesChanged(e);
	}

	private protected override void OnControlThemeChanged()
	{
		base.OnControlThemeChanged();

		var count = VisualChildren.Count;
		for (var i = 0; i < count; ++i)
		{
			if (VisualChildren[i] is StyledElement child &&
				(child.TemplatedParent == this))
			{
				child.OnTemplatedParentControlThemeChanged();
			}
		}
	}

	#endregion

	#region Events

	/// <summary>
	/// Raised when the control's template is applied.
	/// </summary>
	public event EventHandler<TemplateAppliedEventArgs>? TemplateApplied
	{
		add => AddHandler(TemplateAppliedEvent, value);
		remove => RemoveHandler(TemplateAppliedEvent, value);
	}

	#endregion
}