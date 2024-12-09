#region References

using System;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

#endregion

namespace Cornerstone.Avalonia.Converters;

public static class ImageConverters
{
	#region Fields

	public static readonly FuncValueConverter<byte[], Bitmap> ToBitmap = new(BytesToBitmap);

	#endregion

	#region Methods

	public static Bitmap BytesToBitmap(this byte[] data)
	{
		if (data == null)
		{
			return null;
		}

		using var stream = new MemoryStream(data);
		var image = new Bitmap(stream);
		return image;
	}
	
	#endregion
}