param
(
	[Parameter()]
	[string] $Configuration = "Release",
	[Parameter()]
	[string] $BuildNumber = "+",
	[Parameter()]
	[string] $VersionSuffix = ""
)

Clear-Host

$ErrorActionPreference = "Stop"
$watch = [System.Diagnostics.Stopwatch]::StartNew()
$scriptPath = $PSScriptRoot
$productName = "Cornerstone"

if ($scriptPath.Length -le 0)
{
	$scriptPath = "C:\Workspaces\GitHub\$productName"
}

try
{
	Push-Location $scriptPath
	
	# Prepare the build for versioning!
	$newVersion = & "$scriptPath\IncrementVersion.ps1" -Build $BuildNumber
	# Open-File "$scriptPath\IncrementVersion.ps1"
	# $BuildNumber = 3
	
	$nugetVersion = ([Version] $newVersion).ToString(3)
	
	$msbuild = "C:\Program Files\Microsoft Visual Studio\2022\Professional\Msbuild\Current\Bin\MSBuild.exe"
	& $msbuild "$scriptPath\$productName.sln" /p:Configuration="$Configuration" /t:Rebuild /v:m /m
	
	if ($VersionSuffix.Length -gt 0)
	{
		$nugetVersion = "$nugetVersion-$VersionSuffix"
	}
	
	if ($LASTEXITCODE -ne 0)
	{
		Write-Host "Build has failed! " $LASTEXITCODE $watch.Elapsed -ForegroundColor Red
		exit $LASTEXITCODE
	}
	
	Write-Host
	Write-Host "Build: " $watch.Elapsed -ForegroundColor Yellow
}
catch
{
	Write-Host $_.Exception.ToString() -ForegroundColor Red
	Write-Host "Build Failed:" $watch.Elapsed -ForegroundColor Red
	exit $LASTEXITCODE
}
finally
{
	Pop-Location
}