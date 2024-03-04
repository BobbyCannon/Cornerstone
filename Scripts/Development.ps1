$ErrorActionPreference = 'Stop'

This should not be ran as a file

Import-Module WebAdministration
Import-Module WebAdministration -SkipEditionCheck

iisreset

nuget restore "C:\Workspaces\GitHub\Cornerstone\Cornerstone.sln"

& C:\Workspaces\GitHub\Cornerstone\Build.ps1 -BuildNumber 3
# Open-File C:\Workspaces\GitHub\Cornerstone\Build.ps1

& C:\Workspaces\GitHub\Cornerstone\Publish.ps1 -Preview
# Open-File C:\Workspaces\GitHub\Cornerstone\Publish.ps1
