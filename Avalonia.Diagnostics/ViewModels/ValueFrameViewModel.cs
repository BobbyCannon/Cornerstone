#region References

using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;
using Cornerstone.Presentation;

#endregion

namespace Avalonia.Diagnostics.ViewModels;

public class ValueFrameViewModel : ViewModel
{
	#region Fields

	private bool _isActive;
	private bool _isVisible;
	private readonly IValueFrameDiagnostic _valueFrame;

	#endregion

	#region Constructors

	public ValueFrameViewModel(StyledElement styledElement, IValueFrameDiagnostic valueFrame, IClipboard clipboard)
	{
		_valueFrame = valueFrame;
		IsVisible = true;

		var source = SourceToString(_valueFrame.Source);
		Description = (_valueFrame.Type, source) switch
		{
			(IValueFrameDiagnostic.FrameType.Local, _) => "Local Values " + source,
			(IValueFrameDiagnostic.FrameType.Template, _) => "Template " + source,
			(IValueFrameDiagnostic.FrameType.Theme, _) => "Theme " + source,
			(_, { Length: > 0 }) => source,
			_ => _valueFrame.Priority.ToString()
		};

		Setters = [];

		foreach (var (setterProperty, setterValue) in valueFrame.Values)
		{
			var resourceInfo = GetResourceInfo(setterValue);

			SetterViewModel setterVm;

			if (resourceInfo.HasValue)
			{
				var resourceKey = resourceInfo.Value.resourceKey;
				var resourceValue = styledElement.FindResource(resourceKey);

				setterVm = new ResourceSetterViewModel(setterProperty, resourceKey, resourceValue,
					resourceInfo.Value.isDynamic, clipboard);
			}
			else
			{
				var isBinding = IsBinding(setterValue);

				if (isBinding)
				{
					setterVm = new BindingSetterViewModel(setterProperty, setterValue, clipboard);
				}
				else
				{
					setterVm = new SetterViewModel(setterProperty, setterValue, clipboard);
				}
			}
			Setters.Add(setterVm);
		}

		Update();
	}

	#endregion

	#region Properties

	public string Description { get; }

	public bool IsActive
	{
		get => _isActive;
		set => SetProperty(ref _isActive, value);
	}

	public bool IsVisible
	{
		get => _isVisible;
		set => SetProperty(ref _isVisible, value);
	}

	public List<SetterViewModel> Setters { get; }

	#endregion

	#region Methods

	public void Update()
	{
		IsActive = _valueFrame.IsActive;
	}

	private static (object resourceKey, bool isDynamic)? GetResourceInfo(object value)
	{
		if (value is StaticResourceExtension staticResource
			&& (staticResource.ResourceKey != null))
		{
			return (staticResource.ResourceKey, false);
		}
		if (value is DynamicResourceExtension dynamicResource
			&& (dynamicResource.ResourceKey != null))
		{
			return (dynamicResource.ResourceKey, true);
		}

		return null;
	}

	private static bool IsBinding(object value)
	{
		switch (value)
		{
			case Binding:
			case CompiledBindingExtension:
			case TemplateBinding:
				return true;
		}

		return false;
	}

	private string SourceToString(object source)
	{
		if (source is Style style)
		{
			StyleBase currentStyle = style;
			var selectors = new Stack<string>();

			while (currentStyle is not null)
			{
				if (currentStyle is Style { Selector: { } selector })
				{
					selectors.Push(selector.ToString());
				}
				if (currentStyle is ControlTheme theme)
				{
					selectors.Push("Theme " + theme.TargetType?.Name);
				}

				currentStyle = currentStyle.Parent as StyleBase;
			}

			return string.Concat(selectors).Replace("^", "");
		}
		if (source is ControlTheme controlTheme)
		{
			return controlTheme.TargetType?.Name;
		}
		if (source is StyledElement styledElement)
		{
			return styledElement.StyleKey?.Name;
		}

		return null;
	}

	#endregion
}