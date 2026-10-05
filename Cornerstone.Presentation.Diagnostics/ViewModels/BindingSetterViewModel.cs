#region References

using System;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Input.Platform;
using Cornerstone.Presentation.Markup.Xaml.MarkupExtensions;
using Cornerstone.Presentation.Media;

#endregion

namespace Cornerstone.Presentation.Diagnostics.ViewModels;

public class BindingSetterViewModel : SetterViewModel
{
	#region Constructors

	public BindingSetterViewModel(PresentationProperty property, object value, IClipboard clipboard) : base(property, value, clipboard)
	{
		switch (value)
		{
			case Binding binding:
				Path = binding.Path;
				Tint = Brushes.CornflowerBlue;
				ValueTypeTooltip = "Reflection Binding";

				break;
			case CompiledBindingExtension binding:
				Path = binding.Path.ToString();
				Tint = Brushes.DarkGreen;
				ValueTypeTooltip = "Compiled Binding";

				break;
			case TemplateBinding binding:
				if (binding.Property is PresentationProperty templateProperty)
				{
					Path = $"{templateProperty.OwnerType.Name}.{templateProperty.Name}";
				}
				else
				{
					Path = "Unassigned";
				}

				Tint = Brushes.OrangeRed;
				ValueTypeTooltip = "Template Binding";

				break;
			default:
				throw new ArgumentException("Invalid binding type", nameof(value));
		}
	}

	#endregion

	#region Properties

	public string Path { get; }

	public IBrush Tint { get; }

	public string ValueTypeTooltip { get; }

	#endregion

	#region Methods

	public override void CopyValue()
	{
		CopyToClipboard(Path);
	}

	#endregion
}