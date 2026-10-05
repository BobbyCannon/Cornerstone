#region References

using System.IO;
using Cornerstone.Extensions;
using Cornerstone.Presentation.Media.Imaging;

#endregion

namespace Cornerstone.Presentation.Data.Converters;

public static class ImageConverters
{
	#region Fields

	public static readonly FuncValueConverter<string, Bitmap> ToBitmapFromBase64;
	public static readonly FuncValueConverter<byte[], Bitmap> ToBitmap;

	#endregion

	#region Constructors

	static ImageConverters()
	{
		ToBitmapFromBase64 = new(Base64StringToBitmap);
		ToBitmap = new(BytesToBitmap);
	}

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
