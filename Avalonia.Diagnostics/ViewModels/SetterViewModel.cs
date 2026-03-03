#region References

using Avalonia.Input.Platform;
using Cornerstone.Presentation;

#endregion

namespace Avalonia.Diagnostics.ViewModels;

public class SetterViewModel : ViewModel
{
	#region Fields

	private readonly IClipboard _clipboard;
	private bool _isActive;
	private bool _isVisible;

	#endregion

	#region Constructors

	public SetterViewModel(AvaloniaProperty property, object value, IClipboard clipboard)
	{
		Property = property;
		Name = property.Name;
		Value = value;
		IsActive = true;
		IsVisible = true;

		_clipboard = clipboard;
	}

	#endregion

	#region Properties

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

	public string Name { get; }

	public AvaloniaProperty Property { get; }

	public object Value { get; }

	#endregion

	#region Methods

	public void CopyPropertyName()
	{
		CopyToClipboard(Property.Name);
	}

	public virtual void CopyValue()
	{
		var textToCopy = Value?.ToString();

		if (textToCopy is null)
		{
			return;
		}

		CopyToClipboard(textToCopy);
	}

	protected void CopyToClipboard(string value)
	{
		_clipboard?.SetTextAsync(value);
	}

	#endregion
}