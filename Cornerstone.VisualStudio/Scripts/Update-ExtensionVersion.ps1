<#
.SYNOPSIS
    Bumps Cornerstone VSIX / assembly version stamps in lockstep (Major.Minor.Build only).

.DESCRIPTION
    Updates:
      - Directory.Build.props              <Version>
      - source.extension.vsixmanifest      Identity @Version
      - CornerstonePackage.cs              InstalledProductRegistration product version

    Versions are always three-part: Major.Minor.Build (no revision).

    Major/Minor default to the current values (-2 = keep).
    Build defaults to day-of-year (-1). Use -2 for 0, or pass an explicit non-negative Build.

.EXAMPLE
    .\scripts\Update-ExtensionVersion.ps1
    Auto day-of-year build bump from current major.minor

.EXAMPLE
    .\scripts\Update-ExtensionVersion.ps1 -Major 1 -Minor 2 -Build 2
    Fixed marketing release 1.2.2

.EXAMPLE
    .\scripts\Update-ExtensionVersion.ps1 -Major 1 -Minor 2 -WhatIf
    Dry run
#>
param(
	[int] $Major = -2,
	[int] $Minor = -2,
	[int] $Build = -1,
	[switch] $WhatIf
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$propsPath = Join-Path $repoRoot 'Directory.Build.props'
$vsixManifestPath = Join-Path $repoRoot 'Cornerstone.VisualStudio\source.extension.vsixmanifest'
$packageCsPath = Join-Path $repoRoot 'Cornerstone.VisualStudio\CornerstonePackage.cs'

function Get-VersionFromText {
	param([string] $Text, [string] $Pattern)
	$match = [regex]::Match($Text, $Pattern)
	if ($match.Success) {
		return $match.Groups[1].Value
	}
	return $null
}

function Read-CurrentVersion {
	if (Test-Path $propsPath) {
		$propsText = Get-Content -LiteralPath $propsPath -Raw
		$fromProps = Get-VersionFromText -Text $propsText -Pattern '<Version>\s*([^<]+?)\s*</Version>'
		if ($fromProps) {
			return $fromProps.Trim()
		}
	}

	if (Test-Path $vsixManifestPath) {
		$manifestText = Get-Content -LiteralPath $vsixManifestPath -Raw
		$fromManifest = Get-VersionFromText -Text $manifestText -Pattern 'Identity\b[^>]*\bVersion="([^"]+)"'
		if ($fromManifest) {
			return $fromManifest.Trim()
		}
	}

	return '1.0.0'
}

function Split-VersionParts {
	param([string] $Version)
	$parts = $Version.Split('.')
	$major = if ($parts.Length -ge 1 -and $parts[0] -match '^\d+$') { [int]$parts[0] } else { 1 }
	$minor = if ($parts.Length -ge 2 -and $parts[1] -match '^\d+$') { [int]$parts[1] } else { 0 }
	$build = if ($parts.Length -ge 3 -and $parts[2] -match '^\d+$') { [int]$parts[2] } else { 0 }
	# Ignore any 4th+ revision segment if present in legacy stamps.
	return @{
		Major = $major
		Minor = $minor
		Build = $build
	}
}

function Format-ThreePartVersion {
	param(
		[int] $Major,
		[int] $Minor,
		[int] $Build
	)
	return "$Major.$Minor.$Build"
}

function Assert-ThreePartVersion {
	param([string] $Version)
	if ($Version -notmatch '^\d+\.\d+\.\d+$') {
		throw "Version must be Major.Minor.Build only (got '$Version')."
	}
}

function Set-FileContentIfChanged {
	param(
		[string] $Path,
		[string] $OldText,
		[string] $NewText,
		[string] $Label,
		[string] $OldVersion,
		[string] $NewVersion
	)

	if ($OldText -eq $NewText) {
		Write-Host ("  {0,-40} {1} (unchanged)" -f $Label, $NewVersion) -ForegroundColor DarkGray
		return $false
	}

	Write-Host ("  {0,-40} {1} -> {2}" -f $Label, $OldVersion, $NewVersion) -ForegroundColor Cyan
	if (-not $WhatIf) {
		# Preserve existing newline style (no extra trailing newline rewrite surprises)
		$utf8NoBom = New-Object System.Text.UTF8Encoding $false
		[System.IO.File]::WriteAllText($Path, $NewText, $utf8NoBom)
	}
	return $true
}

# --- Resolve current + compute new version ---
$currentVersion = Read-CurrentVersion
$current = Split-VersionParts -Version $currentVersion

$newMajor = if ($Major -ge 0) { $Major } else { $current.Major }
$newMinor = if ($Minor -ge 0) { $Minor } else { $current.Minor }

if ($Build -ge 0) {
	$newBuild = $Build
}
elseif ($Build -eq -1) {
	$yearStart = Get-Date -Year ([DateTime]::Now.Year) -Month 1 -Day 1
	$newBuild = [int][Math]::Floor(([DateTime]::Now.Date - $yearStart.Date).TotalDays)
}
else {
	# -2 or other: zero
	$newBuild = 0
}

$newVersion = Format-ThreePartVersion -Major $newMajor -Minor $newMinor -Build $newBuild
Assert-ThreePartVersion -Version $newVersion

Write-Host ""
Write-Host "Cornerstone extension version bump" -ForegroundColor White
Write-Host "  Current : $currentVersion"
Write-Host "  New     : $newVersion"
if ($WhatIf) {
	Write-Host "  Mode    : WhatIf (no files will be written)" -ForegroundColor Yellow
}
Write-Host ""
Write-Host "Targets:" -ForegroundColor White

$changed = 0

# --- Directory.Build.props ---
if (-not (Test-Path -LiteralPath $propsPath)) {
	throw "Missing Directory.Build.props at $propsPath"
}
$propsText = Get-Content -LiteralPath $propsPath -Raw
$propsOldVersion = Get-VersionFromText -Text $propsText -Pattern '<Version>\s*([^<]+?)\s*</Version>'
if ($propsOldVersion) {
	$propsNewText = [regex]::Replace(
		$propsText,
		'(<Version>)\s*[^<]+?\s*(</Version>)',
		{ param($m) $m.Groups[1].Value + $newVersion + $m.Groups[2].Value },
		1)
	$oldDisplay = $propsOldVersion.Trim()
}
else {
	# Insert Version after opening PropertyGroup
	$propsNewText = [regex]::Replace(
		$propsText,
		'(<PropertyGroup>\r?\n)',
		{ param($m) $m.Groups[1].Value + "`t`t<Version>$newVersion</Version>`r`n" },
		1)
	$oldDisplay = '(missing)'
}
if (Set-FileContentIfChanged -Path $propsPath -OldText $propsText -NewText $propsNewText `
		-Label 'Directory.Build.props <Version>' -OldVersion $oldDisplay -NewVersion $newVersion) {
	$changed++
}

# --- source.extension.vsixmanifest ---
if (-not (Test-Path -LiteralPath $vsixManifestPath)) {
	throw "Missing vsixmanifest at $vsixManifestPath"
}
$manifestText = Get-Content -LiteralPath $vsixManifestPath -Raw
$manifestOldVersion = Get-VersionFromText -Text $manifestText -Pattern 'Identity\b[^>]*\bVersion="([^"]+)"'
if (-not $manifestOldVersion) {
	throw "Could not find Identity Version in $vsixManifestPath"
}
$manifestNewText = [regex]::Replace(
	$manifestText,
	'(Identity\b[^>]*\bVersion=")[^"]+(")',
	{ param($m) $m.Groups[1].Value + $newVersion + $m.Groups[2].Value },
	1)
if (Set-FileContentIfChanged -Path $vsixManifestPath -OldText $manifestText -NewText $manifestNewText `
		-Label 'source.extension.vsixmanifest' -OldVersion $manifestOldVersion -NewVersion $newVersion) {
	$changed++
}

# --- CornerstonePackage.cs InstalledProductRegistration ---
if (-not (Test-Path -LiteralPath $packageCsPath)) {
	throw "Missing package source at $packageCsPath"
}
$csText = Get-Content -LiteralPath $packageCsPath -Raw
$csPattern = 'InstalledProductRegistration\s*\(\s*"#110"\s*,\s*"#112"\s*,\s*"([^"]+)"'
$csOldVersion = Get-VersionFromText -Text $csText -Pattern $csPattern
if (-not $csOldVersion) {
	throw "Could not find InstalledProductRegistration version in $packageCsPath"
}
$csNewText = [regex]::Replace(
	$csText,
	'(InstalledProductRegistration\s*\(\s*"#110"\s*,\s*"#112"\s*,\s*")[^"]+(")',
	{ param($m) $m.Groups[1].Value + $newVersion + $m.Groups[2].Value },
	1)
if (Set-FileContentIfChanged -Path $packageCsPath -OldText $csText -NewText $csNewText `
		-Label 'CornerstonePackage.cs (About PID)' -OldVersion $csOldVersion -NewVersion $newVersion) {
	$changed++
}

Write-Host ""
if ($WhatIf) {
	Write-Host "WhatIf complete. $changed file(s) would change." -ForegroundColor Yellow
}
elseif ($changed -eq 0) {
	Write-Host "Already at $newVersion - no files changed." -ForegroundColor DarkGray
}
else {
	Write-Host "Updated $changed file(s) to $newVersion." -ForegroundColor Green
}
Write-Host ""
