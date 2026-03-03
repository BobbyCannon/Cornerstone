#region References

using Avalonia.Input.Platform;
using Avalonia.Media;

#endregion

namespace Avalonia.Diagnostics.ViewModels;

public class ResourceSetterViewModel : SetterViewModel
{
	#region Constructors

	public ResourceSetterViewModel(AvaloniaProperty property, object resourceKey, object resourceValue, bool isDynamic, IClipboard clipboard) : base(property, resourceValue, clipboard)
	{
		Key = resourceKey;
		Tint = isDynamic ? Brushes.Orange : Brushes.Brown;
		ValueTypeTooltip = isDynamic ? "Dynamic Resource" : "Static Resource";
	}

	#endregion

	#region Properties

	public object Key { get; }

	public IBrush Tint { get; }

	public string ValueTypeTooltip { get; }

	#endregion

	#region Methods

	public void CopyResourceKey()
	{
		var textToCopy = Key?.ToString();

		if (textToCopy is null)
		{
			return;
		}

		CopyToClipboard(textToCopy);
	}

	#endregion
}