# Table of Contents

- [EF 10 + WinRT (net10-windows)](#ef001-ef-10-winrt-startup)
- [WebView native layering](#wv001-webview-native-airspace)

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

This issue has nothing to do with Cornerstone.Presentation’s startup timing or lifecycle. The exception
is thrown the very first time the SqliteConnection type is initialized (its static constructor),
which happens the moment any code first touches a connection or EF Core SQLite context.
That can be:

- inside Keystone.LoadLifecycle() / StartLifecycle()
- in a view-model constructor
- on the first query
- or even in a background service

…and the result is identical. Cornerstone.Presentation’s Startup event, OnFrameworkInitializationCompleted,
dispatcher readiness, etc. are irrelevant. 

The only factors that matter are:

- You are running an unpackaged Windows process, and 
- Your project pulls in the Windows SDK / CsWinRT projection (normally because of a net10.0-windows… TFM), and  
- You are using Microsoft.Data.Sqlite 10.x.

Once those three conditions are true, the first SqliteConnection construction will hit
the ApplicationData.Current probe and throw. Changing when in the Cornerstone.Presentation lifetime you
open the database does not avoid it.

---

# WV001 - WebView native airspace

How it works: [Presentation/NativeLayering.md](Presentation/NativeLayering.md). Native WebView sits **under** the Skia plane. Overlay chrome paints on live web; hole clicks go to native.

`NativeBehindComposition` defaults **on** (Win32, Android, iOS, macOS, Wayland). Set it false for child-on-top (native above Skia). X11 has no native-behind path. There is no freeze-frame pause; overlay chrome has to be a later sibling on a platform where native-behind is on.