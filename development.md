# Cornerstone

Ideas

- Ability to track sync times (some auto table for tracking Client/Server LastSyncedOn)

## Database Migrations

Add-Migration InitialDatabase -Project Sample.Server.Data.Sql -StartupProject Sample.Server.Website
Add-Migration InitialDatabase -Project Sample.Server.Data.Sqlite -StartupProject Cornerstone.IntegrationTests

Add-Migration InitialDatabase -Project Sample.Client.Data.Sql -StartupProject Cornerstone.IntegrationTests
Add-Migration InitialDatabase -Project Sample.Client.Data.Sqlite -StartupProject Cornerstone.IntegrationTests


# Platform Links

## Android

[App Manifest](https://developer.android.com/guide/topics/manifest/manifest-intro)

[WebView](https://developer.android.com/develop/ui/views/layout/webapps/webview)

## Windows

[WebView2](https://learn.microsoft.com/en-us/microsoft-edge/webview2)


# Reference Links

## Nuget Details

[Create Packages](https://learn.microsoft.com/en-us/nuget/create-packages/creating-a-package-msbuild)

## Unit Test

[Run Settings](https://learn.microsoft.com/en-us/visualstudio/test/configure-unit-tests-by-using-a-dot-runsettings-file?branch=release-16.4&view=vs-2019)


## Maui Memory Leaks

https://github.com/dotnet/maui/issues/9162

https://github.com/Keflon/FunctionZero.Maui.Controls

