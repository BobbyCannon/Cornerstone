# Cornerstone

Cornerstone in as development framework to help speed up and make building applications and systems
much easier.

## Database Migrations

Add-Migration InitialDatabase -Project Sample.Server.Data.Sql -StartupProject Sample.Server.Website
Add-Migration InitialDatabase -Project Sample.Server.Data.Sqlite -StartupProject Cornerstone.IntegrationTests

Add-Migration InitialDatabase -Project Sample.Client.Data.Sql -StartupProject Cornerstone.IntegrationTests
Add-Migration InitialDatabase -Project Sample.Client.Data.Sqlite -StartupProject Cornerstone.IntegrationTests

# Reference Links

## Nuget Details
https://learn.microsoft.com/en-us/nuget/create-packages/creating-a-package-msbuild

## Unit Test
https://learn.microsoft.com/en-us/visualstudio/test/configure-unit-tests-by-using-a-dot-runsettings-file?branch=release-16.4&view=vs-2019


## Maui Memory Leaks

https://github.com/dotnet/maui/issues/9162

https://github.com/Keflon/FunctionZero.Maui.Controls

