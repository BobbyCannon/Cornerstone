# Table of Contents

- [EF 10 + WinRT (net10-windows)](#ef001-ef-10-winrt-startup)
- [WebView native airspace (overlay)](#wv001-webview-native-airspace)

---

GitHub’s heading-anchor algorithm (the practical rules)When GitHub renders a heading it creates
an id attribute using these steps:Convert the heading text to lowercase. Remove all punctuation
/ special characters (keep letters, numbers, spaces, and hyphens). 
Replace spaces with hyphens (-). Collapse consecutive hyphens. Strip leading/trailing hyphens.
If the resulting ID already exists in the document, append -1, -2, etc.

---

# EF001 - EF 10 + WinRT Startup

This issue has been fixed in EF v11+. This also applies to Windows desktop apps net10-windows[v*].

### Catch InvalidOperationException for unpackaged WinUI3 apps in SqliteConnection #38304

https://github.com/dotnet/efcore/pull/38304

This issue has nothing to do with Avalonia’s startup timing or lifecycle. The exception
is thrown the very first time the SqliteConnection type is initialized (its static constructor),
which happens the moment any code first touches a connection or EF Core SQLite context.
That can be:

- inside Keystone.LoadLifecycle() / StartLifecycle()
- in a view-model constructor
- on the first query
- or even in a background service

…and the result is identical. Avalonia’s Startup event, OnFrameworkInitializationCompleted,
dispatcher readiness, etc. are irrelevant. 

The only factors that matter are:

- You are running an unpackaged Windows process, and 
- Your project pulls in the Windows SDK / CsWinRT projection (normally because of a net10.0-windows… TFM), and  
- You are using Microsoft.Data.Sqlite 10.x.

Once those three conditions are true, the first SqliteConnection construction will hit
the ApplicationData.Current probe and throw. Changing when in the Avalonia lifetime you
open the database does not avoid it.

---

# WV001 - WebView native airspace

Native WebView (Android WebView, iOS WKWebView, Windows WebView2) always paints above
Avalonia content. You cannot place Avalonia controls over a live native WebView surface.

### Workaround: `WebView.IsPaused`

Set `WebView.IsPaused = true` (optional `BlurWhenPaused`):

1. Captures a PNG snapshot of the web surface (owned by `WebView`).
2. Shows that image in the control and hides the native surface only (engine stays alive).
3. Avalonia siblings placed **after** / above the WebView in Z-order can paint over the region.
4. Set `IsPaused = false` to restore the live WebView. Significant resize resumes by default
   (`ResumeOnResize`).

Do **not** set `IsVisible=false` on the whole WebView to clear airspace — use pause instead.

Tradeoffs: the page freezes while paused; media freezes visually; capture can fail (solid
fallback still clears airspace).