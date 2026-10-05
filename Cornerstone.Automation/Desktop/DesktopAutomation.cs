#region References

using System;
using System.Runtime.InteropServices;
using Interop.UIAutomationClient;

#endregion

namespace Cornerstone.Automation.Desktop;

/// <summary>
/// Shared UI Automation COM instance. Creating CUIAutomation per call is expensive and
/// contributes to COM errors when several browsers walk the tree at once.
/// </summary>
internal static class DesktopAutomation
{
	#region Fields

	private static readonly object Sync = new();
	private static IUIAutomation _instance;

	#endregion

	#region Properties

	public static IUIAutomation Instance
	{
		get
		{
			lock (Sync)
			{
				return _instance ??= Create();
			}
		}
	}

	#endregion

	#region Methods

	public static void Use(Action<IUIAutomation> action)
	{
		lock (Sync)
		{
			action(Instance);
		}
	}

	public static T Use<T>(Func<IUIAutomation, T> action)
	{
		lock (Sync)
		{
			return action(Instance);
		}
	}

	private static IUIAutomation Create()
	{
		try
		{
			return new CUIAutomation8Class();
		}
		catch (COMException)
		{
			return new CUIAutomationClass();
		}
	}

	#endregion
}
