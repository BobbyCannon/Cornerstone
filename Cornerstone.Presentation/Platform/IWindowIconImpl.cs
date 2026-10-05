#region References

using System.IO;
using Cornerstone.Presentation.Metadata;

#endregion

namespace Cornerstone.Presentation.Platform;

[Unstable]
public interface IWindowIconImpl
{
	#region Methods

	void Save(Stream outputStream);

	#endregion
}