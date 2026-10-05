#region References

using System.IO;
using Cornerstone.Presentation.Media.Imaging;
using Cornerstone.Presentation.Platform;

#endregion

namespace Cornerstone.Presentation.Controls.Chrome;

/// <summary>
/// Represents an icon for a window.
/// </summary>
public class WindowIcon
{
	#region Fields

	private Bitmap _bitmap;

	#endregion

	#region Constructors

	public WindowIcon(Bitmap bitmap)
	{
		_bitmap = bitmap;
		PlatformImpl = PresentationLocator.Current.GetRequiredService<IPlatformIconLoader>().LoadIcon(bitmap.PlatformImpl.Item);
	}

	public WindowIcon(string fileName)
	{
		PlatformImpl = PresentationLocator.Current.GetRequiredService<IPlatformIconLoader>().LoadIcon(fileName);
	}

	public WindowIcon(Stream stream)
	{
		PlatformImpl = PresentationLocator.Current.GetRequiredService<IPlatformIconLoader>().LoadIcon(stream);
	}

	#endregion

	#region Properties

	internal IWindowIconImpl PlatformImpl { get; }

	#endregion

	#region Methods

	public void Save(Stream stream)
	{
		PlatformImpl.Save(stream);
	}

	/// <summary>
	/// Returns a raster of the already-loaded icon. Decodes once and reuses the same bitmap.
	/// </summary>
	public Bitmap ToBitmap()
	{
		if (_bitmap != null)
		{
			return _bitmap;
		}

		using var stream = new MemoryStream();
		Save(stream);
		if (stream.Length == 0)
		{
			return null;
		}

		stream.Position = 0;
		_bitmap = new Bitmap(stream);
		return _bitmap;
	}

	#endregion
}