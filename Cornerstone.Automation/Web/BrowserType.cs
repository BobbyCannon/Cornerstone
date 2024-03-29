﻿#region References

using System;

#endregion

namespace Cornerstone.Automation.Web;

/// <summary>
/// The type of the browser.
/// </summary>
[Flags]
public enum BrowserType
{
	/// <summary>
	/// No browsers, empty.
	/// </summary>
	None = 0,

	/// <summary>
	/// Represents a Chrome browser.
	/// </summary>
	Chrome = 0x01,

	/// <summary>
	/// Represents an Edge (Chromium) browser.
	/// </summary>
	Edge = 0x02,

	/// <summary>
	/// Represents a Firefox browser.
	/// </summary>
	Firefox = 0x04,

	/// <summary>
	/// Represents all browser types.
	/// </summary>
	All = 0xFF
}