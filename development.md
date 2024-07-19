# Cornerstone

Ideas

- Ability to track sync times (some auto table for tracking Client/Server LastSyncedOn)

## Database Migrations

Add-Migration InitialDatabase -Project Sample.Server.Data.Sql -StartupProject Sample.Server.Website
Add-Migration InitialDatabase -Project Sample.Server.Data.Sqlite -StartupProject Cornerstone.IntegrationTests

Add-Migration InitialDatabase -Project Sample.Client.Data.Sql -StartupProject Cornerstone.IntegrationTests
Add-Migration InitialDatabase -Project Sample.Client.Data.Sqlite -StartupProject Cornerstone.IntegrationTests

# Reference Links

## Avalonia

[Styles, Control Themes, etc](https://docs.avaloniaui.net/docs/guides/styles-and-resources/setter-precedence)

[CherylUI (nice UI)](https://github.com/kikipoulet/CherylUI)

## Nuget Details

[Create Packages](https://learn.microsoft.com/en-us/nuget/create-packages/creating-a-package-msbuild)

## Unit Test

[Run Settings](https://learn.microsoft.com/en-us/visualstudio/test/configure-unit-tests-by-using-a-dot-runsettings-file?branch=release-16.4&view=vs-2019)


## Maui Memory Leaks

https://github.com/dotnet/maui/issues/9162

https://github.com/Keflon/FunctionZero.Maui.Controls

