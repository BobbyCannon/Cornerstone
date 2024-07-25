#region References

using Avalonia.Controls;

#endregion

namespace Cornerstone.Avalonia.Android.Core;

partial class AndroidWebViewCore
{
	#region Methods

	internal TopLevel GetTopLevel()
	{
		return TopLevel.GetTopLevel(_handler);
	}

	#endregion
}