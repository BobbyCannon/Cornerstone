#region References

using System;
using System.Diagnostics;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView.Core.Helpers;

public class OpenUriHelper
{
	#region Methods

	public static void OpenInProcess(Uri uri)
	{
		using (var launchBrowser = new Process())
		{
			launchBrowser.StartInfo.UseShellExecute = true;
			launchBrowser.StartInfo.FileName = uri.ToString();
			launchBrowser.Start();
		}
	}

	#endregion
}