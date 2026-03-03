#region References

using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Avalonia.Converters;

public static class ImageConverters
{
	#region Fields

	public static readonly FuncValueConverter<string, Bitmap> ToBitmapFromBase64 = new(Base64StringToBitmap);
	public static readonly FuncValueConverter<byte[], Bitmap> ToBitmap = new(BytesToBitmap);

	#endregion

	#region Methods

	public static Bitmap Base64StringToBitmap(this string data)
	{
		if (string.IsNullOrWhiteSpace(data))
		{
			return null;
		}

		var bytes = StringExtensions.FromBase64StringToByteArray(data);
		return bytes.BytesToBitmap();
	}

	public static Bitmap BytesToBitmap(this byte[] data)
	{
		if (data is not { Length: > 0 })
		{
			return null;
		}

		using var stream = new MemoryStream(data);
		var image = new Bitmap(stream);
		return image;
	}

	#endregion
}