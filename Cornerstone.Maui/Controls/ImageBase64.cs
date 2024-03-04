#region References

using System.IO;
using Microsoft.Maui.Controls;

#endregion

namespace Cornerstone.Maui.Controls;

public class ImageBase64 : Image
{
	#region Fields

	public static readonly BindableProperty Base64SourceProperty = BindableProperty.Create(
		nameof(Base64Source), typeof(string), typeof(ImageBase64), string.Empty,
		propertyChanged: OnBase64SourceChanged
	);

	#endregion

	#region Properties

	public string Base64Source
	{
		get => (string) GetValue(Base64SourceProperty);
		set => SetValue(Base64SourceProperty, value);
	}

	#endregion

	#region Methods

	public void Refresh()
	{
		Refresh(Base64Source);
	}

	public void Refresh(string value)
	{
		const string prefix = "data:image/png;base64,";

		if (value.StartsWith(prefix))
		{
			value = value.Substring(prefix.Length);
		}

		var imageBytes = System.Convert.FromBase64String(value);
		var stream = new MemoryStream(imageBytes);
		var imageSource = ImageSource.FromStream(() => stream);

		Source = imageSource;
	}

	private static void OnBase64SourceChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (newValue is not string value)
		{
			return;
		}

		var image = (ImageBase64) bindable;

		image.Refresh(value);
	}

	#endregion
}