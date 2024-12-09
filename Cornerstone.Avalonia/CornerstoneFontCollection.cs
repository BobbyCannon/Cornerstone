#region References

using System;
using Avalonia.Media.Fonts;

#endregion

namespace Cornerstone.Avalonia;

public sealed class CornerstoneFontCollection : EmbeddedFontCollection
{
	#region Constructors

	public CornerstoneFontCollection() : base(
		new Uri("fonts:Cornerstone", UriKind.Absolute),
		new Uri("avares://Cornerstone.Avalonia/Assets/Fonts", UriKind.Absolute))
	{
	}

	#endregion
}