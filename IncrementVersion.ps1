[CmdletBinding(PositionalBinding = $false)]

param
(
	[Parameter(Mandatory = $false)]
	[string] $Major,
	[Parameter(Mandatory = $false)]
	[string] $Minor,
	[Parameter(Mandatory = $false)]
	[string] $Build,
	[Parameter(Mandatory = $false)]
	[string] $Revision,
	[Parameter(Mandatory = $false)]
	[DateTime] $DateSince
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

if ($DateSince -eq $null)
{
	$Year = [DateTime]::Now.Year
	$DateSince = [DateTime]::Parse("01/01/$Year").Date
}

# See if the build should be generated.

if ($Build -eq "*")
{
	$Build = [Math]::Floor([DateTime]::Now.Subtract($DateSince).TotalDays)
}

# See if the revision should be generated.

if ($Revision -eq "*")
{
	$Revision = [Math]::Floor([DateTime]::Now.TimeOfDay.TotalSeconds / 2)
}

$file = [System.IO.FileInfo] "$scriptPath\Directory.Build.props"
$fileXml = [xml](Get-Content $file.FullName -Raw)
$versionArray = $fileXml.Project.PropertyGroup.AssemblyVersion.ToString().Split('.')

# Ensure all requires parts are available
if ($Major.Length -gt 0 -and $versionArray.Length -lt 1)
{
	$versionArray += 0;
}
if ($Minor.Length -gt 0 -and $versionArray.Length -lt 2)
{
	$versionArray += 0;
}
if ($Build.Length -gt 0 -and $versionArray.Length -lt 3)
{
	$versionArray += 0;
}
if ($Revision.Length -gt 0 -and $versionArray.Length -lt 4)
{
	$versionArray += 0;
}

try
{
	if ($Major -eq "+")
	{
		$versionArray[0] = ([int] $versionArray[0]) + 1
	}
	elseif($Major.Length -gt 0)
	{
		$versionArray[0] = $Major
	}
	
	if ($Minor -eq "+")
	{
		$versionArray[1] = ([int] $versionArray[1]) + 1
	}
	elseif($Minor.Length -gt 0)
	{
		$versionArray[1] = $Minor
	}
	elseif($Major.Length -gt 0 -and $versionArray.Length -ge 2)
	{
		$Minor = "0"
		$versionArray[1] = $Minor
	}
	
	if ($Build -eq "+")
	{
		$versionArray[2] = ([int] $versionArray[2]) + 1
	}
	elseif($Build.Length -gt 0)
	{
		$versionArray[2] = $Build
	}
	elseif($Minor.Length -gt 0  -and $versionArray.Length -ge 3)
	{
		$Build = "0"
		$versionArray[2] = $Build
	}
	
	if ($Revision -eq "+")
	{
		$versionArray[3] = ([int] $versionArray[3]) + 1
	}
	elseif($Revision.Length -gt 0)
	{
		$versionArray[3] = $Revision
	}
	elseif($Build.Length -gt 0 -and $versionArray.Length -ge 4)
	{
		$Revision = "0"
		$versionArray[3] = $Revision
	}
	
	$version = [String]::Join(".", $versionArray)
	
	Write-Host "Updating Cornerstone version to $version"
	
	if ($fileXml.Project.PropertyGroup.AssemblyVersion -ne $null)
	{
		$fileXml.Project.PropertyGroup.AssemblyVersion = $version
	}
	
	if ($fileXml.Project.PropertyGroup.FileVersion -ne $null)
	{
		$fileXml.Project.PropertyGroup.FileVersion = $version
	}

	if ($fileXml.Project.PropertyGroup.Version -ne $null)
	{
		$fileXml.Project.PropertyGroup.Version = $version
	}
	
	Set-Content -Path $file.FullName -Value (Format-Xml -Data $fileXml.OuterXml) -Encoding utf8

	return $version
	exit $LASTEXITCODE
}
catch
{
	Write-Host $_.Exception.ToString() -ForegroundColor Red
	exit $LASTEXITCODE
}
finally
{
	Pop-Location
}