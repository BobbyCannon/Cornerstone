#region References

using System;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Avalonia;

public class CornerstoneRuntimeInformation : RuntimeInformation
{
	#region Constructors

	static CornerstoneRuntimeInformation()
	{
		Instance = new CornerstoneRuntimeInformation();
	}

	#endregion

	#region Properties

	public static CornerstoneRuntimeInformation Instance { get; }

	#endregion
}