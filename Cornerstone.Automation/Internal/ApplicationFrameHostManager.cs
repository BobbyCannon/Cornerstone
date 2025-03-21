#region References

using System;
using System.Diagnostics;
using Cornerstone.Automation.Desktop.Elements;
using Cornerstone.Extensions;
using Cornerstone.Platforms.Windows;

#endregion

namespace Cornerstone.Automation.Internal;

internal static class ApplicationFrameHostManager
{
	#region Methods

	public static IntPtr Refresh(SafeProcess process, TimeSpan? timeout = null)
	{
		var handle = IntPtr.Zero;

		if (process == null)
		{
			return handle;
		}

		UtilityExtensions.WaitUntil(() =>
			{
				var frameHosts = Process.GetProcessesByName("ApplicationFrameHost");

				foreach (var host in frameHosts)
				{
					using var application = new Application(host);
					application.Refresh();

					foreach (var c in application.Children)
					{
						if (!(c is Window window))
						{
							continue;
						}

						if (window.NativeElement.CurrentProcessId == process.Id)
						{
							window.Dispose();
							handle = window.Handle;
							return true;
						}

						foreach (var cc in c.Children)
						{
							if (!(cc is Window ww))
							{
								continue;
							}

							if (ww.NativeElement.CurrentProcessId != process.Id)
							{
								continue;
							}

							ww.Dispose();
							handle = window.Handle;
							return true;
						}
					}
				}

				return false;
			},
			timeout ?? TimeSpan.Zero,
			25);

		return handle;
	}

	#endregion
}