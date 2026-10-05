#region References

using System.IO;
using Cornerstone.Presentation.Metadata;

#endregion

namespace Cornerstone.Presentation.Platform;

[Unstable]
[PrivateApi]
public interface IPlatformIconLoader
{
	#region Methods

	IWindowIconImpl LoadIcon(string fileName);
	IWindowIconImpl LoadIcon(Stream stream);
	IWindowIconImpl LoadIcon(IBitmapImpl bitmap);

	#endregion
}