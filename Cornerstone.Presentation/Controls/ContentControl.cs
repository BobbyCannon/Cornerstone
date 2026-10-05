#region References

using System.Text;
using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Controls.Metadata;
using Cornerstone.Presentation.Controls.Presenters;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Presentation.Controls.StyleClasses;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Presentation.Controls;

public class ContentControl<T> : ContentControl
	where T : class
{
	#region Fields

	public static readonly StyledProperty<T> ViewModelProperty;

	#endregion

	#region Constructors

	static ContentControl()
	{
		ViewModelProperty = PresentationProperty.Register<ContentControl<T>, T>(nameof(ViewModel));
	}

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
/// Displays <see cref="Content" /> according to an <see cref="IDataTemplate" />.
/// </summary>
[TemplatePart("PART_ContentPresenter", typeof(ContentPresenter))]
[PseudoClasses(pcEmpty)]
public class ContentControl : TemplatedControl, IContentControl, IContentPresenterHost
{
	#region Constants

	private const string pcEmpty = ":empty";

	#endregion

	#region Fields

	/// <summary>
	/// Defines the <see cref="Content" /> property.
	/// </summary>
	public static readonly StyledProperty<object?> ContentProperty;

	/// <summary>
	/// Defines the <see cref="ContentTemplate" /> property.
	/// </summary>
	public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty;

	/// <summary>
	/// Defines the <see cref="HorizontalContentAlignment" /> property.
	/// </summary>
	public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty;

	/// <summary>
	/// Defines the <see cref="VerticalContentAlignment" /> property.
	/// </summary>
	public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty;

	#endregion

	#region Constructors

	public ContentControl()
	{
		UpdatePseudoClasses();
	}

	static ContentControl()
	{
		ContentProperty = PresentationProperty.Register<ContentControl, object>(nameof(Content));
		ContentTemplateProperty = PresentationProperty.Register<ContentControl, IDataTemplate>(nameof(ContentTemplate));
		HorizontalContentAlignmentProperty = PresentationProperty.Register<ContentControl, HorizontalAlignment>(nameof(HorizontalContentAlignment));
		VerticalContentAlignmentProperty = PresentationProperty.Register<ContentControl, VerticalAlignment>(nameof(VerticalContentAlignment));
		TemplateProperty.OverrideDefaultValue<ContentControl>(new FuncControlTemplate((_, ns) => new ContentPresenter
		{
			Name = "PART_ContentPresenter",
			[~BackgroundProperty] = new TemplateBinding(BackgroundProperty),
			[~BackgroundSizingProperty] = new TemplateBinding(BackgroundSizingProperty),
			[~BorderBrushProperty] = new TemplateBinding(BorderBrushProperty),
			[~BorderThicknessProperty] = new TemplateBinding(BorderThicknessProperty),
			[~CornerRadiusProperty] = new TemplateBinding(CornerRadiusProperty),
			[~ContentTemplateProperty] = new TemplateBinding(ContentTemplateProperty),
			[~ContentProperty] = new TemplateBinding(ContentProperty),
			[~PaddingProperty] = new TemplateBinding(PaddingProperty),
			[~VerticalContentAlignmentProperty] = new TemplateBinding(VerticalContentAlignmentProperty),
			[~HorizontalContentAlignmentProperty] = new TemplateBinding(HorizontalContentAlignmentProperty)
		}.RegisterInNameScope(ns)));
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets the content to display.
	/// </summary>
	[Content]
	[DependsOn(nameof(ContentTemplate))]
	public object? Content
	{
		get => GetValue(ContentProperty);
		set => SetValue(ContentProperty, value);
	}

	/// <summary>
	/// Gets or sets the data template used to display the content of the control.
	/// </summary>
	public IDataTemplate? ContentTemplate
	{
		get => GetValue(ContentTemplateProperty);
		set => SetValue(ContentTemplateProperty, value);
	}

	/// <summary>
	/// Gets or sets the horizontal alignment of the content within the control.
	/// </summary>
	public HorizontalAlignment HorizontalContentAlignment
	{
		get => GetValue(HorizontalContentAlignmentProperty);
		set => SetValue(HorizontalContentAlignmentProperty, value);
	}

	/// <summary>
	/// Gets the presenter from the control's template.
	/// </summary>
	public ContentPresenter? Presenter { get; private set; }

	/// <summary>
	/// Gets or sets the vertical alignment of the content within the control.
	/// </summary>
	public VerticalAlignment VerticalContentAlignment
	{
		get => GetValue(VerticalContentAlignmentProperty);
		set => SetValue(VerticalContentAlignmentProperty, value);
	}

	/// <inheritdoc />
	IOldPresentationList<ILogical> IContentPresenterHost.LogicalChildren => LogicalChildren;

	#endregion

	#region Methods

	protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (change.Property == ContentProperty)
		{
			ContentChanged(change);
		}
	}

	/// <summary>
	/// Called when an <see cref="ContentPresenter" /> is registered with the control.
	/// </summary>
	/// <param name="presenter"> The presenter. </param>
	protected virtual bool RegisterContentPresenter(ContentPresenter presenter)
	{
		if (presenter.Name == "PART_ContentPresenter")
		{
			Presenter = presenter;
			return true;
		}

		return false;
	}

	internal override void BuildDebugDisplay(StringBuilder builder, bool includeContent)
	{
		base.BuildDebugDisplay(builder, includeContent);

		if (includeContent)
		{
			DebugDisplayHelper.AppendOptionalValue(builder, nameof(Content), Content, includeContent);
		}
	}

	private void ContentChanged(PresentationPropertyChangedEventArgs e)
	{
		if (e.OldValue is ILogical oldChild)
		{
			LogicalChildren.Remove(oldChild);
		}

		if (e.NewValue is ILogical newChild)
		{
			LogicalChildren.Add(newChild);
		}

		UpdatePseudoClasses();
	}

	/// <inheritdoc />
	bool IContentPresenterHost.RegisterContentPresenter(ContentPresenter presenter)
	{
		return RegisterContentPresenter(presenter);
	}

	private void UpdatePseudoClasses()
	{
		PseudoClasses.Set(pcEmpty, Content is null);
	}

	#endregion
}