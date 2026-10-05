<#
.SYNOPSIS
    Bumps Cornerstone VSIX / assembly version stamps in lockstep.

.DESCRIPTION
    Updates:
      - Directory.Build.props              <Version>
      - source.extension.vsixmanifest      Identity @Version
      - CornerstonePackage.cs              InstalledProductRegistration product version

    Version is Major.Minor.Build or Major.Minor.Build.Revision.
    Each part must be 0 through 65534 (Marketplace limit).

    Major/Minor default to the current values (-2 = keep).
    Build and Revision accept a number, '*', or -1.
    '*' and -1 calculate the part from the clock, same as Scripts\Increment-Version.ps1:
      Build    = days since January 1 of this year
      Revision = half-seconds since midnight
    Build -2 (or any other negative besides -1) writes 0.
    Revision -2 omits the fourth part (Major.Minor.Build only).

.EXAMPLE
    .\scripts\Update-ExtensionVersion.ps1
    Current major.minor, build and revision from the clock

.EXAMPLE
    .\scripts\Update-ExtensionVersion.ps1 -Major 1 -Minor 5 -Build * -Revision *
    Same clock calculation, with an explicit major and minor

.EXAMPLE
    .\scripts\Update-ExtensionVersion.ps1 -Major 1 -Minor 2 -Build 2 -Revision -2
    Fixed three-part release 1.2.2

.EXAMPLE
    .\scripts\Update-ExtensionVersion.ps1 -Major 1 -Minor 2 -WhatIf
    Dry run
#>
param(
	[int] $Major = -2,
	[int] $Minor = -2,
	[string] $Build = '*',
	[string] $Revision = '*',
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
	$revision = if ($parts.Length -ge 4 -and $parts[3] -match '^\d+$') { [int]$parts[3] } else { $null }
	return @{
		Major = $major
		Minor = $minor
		Build = $build
		Revision = $revision
	}
}

function Get-DayOfYearBuild {
	$yearStart = Get-Date -Year ([DateTime]::Now.Year) -Month 1 -Day 1
	return [int][Math]::Floor(([DateTime]::Now.Date - $yearStart.Date).TotalDays)
}

function Get-HalfSecondRevision {
	return [int][Math]::Floor([DateTime]::Now.TimeOfDay.TotalSeconds / 2)
}

function Resolve-VersionPart {
	param(
		[string] $Value,
		[string] $Name,
		[scriptblock] $Auto,
		[switch] $AllowOmit
	)

	$text = ''
	if ($null -ne $Value) {
		$text = $Value.Trim()
	}

	if (($text -eq '*') -or ($text -eq '-1')) {
		$part = [int](& $Auto)
	}
	elseif ($AllowOmit -and (($text -eq '') -or ($text -eq '-2'))) {
		return $null
	}
	elseif ($text -match '^-?\d+$') {
		$part = [int]$text
		if ($part -lt 0) {
			return 0
		}
	}
	else {
		throw "$Name must be a number, '*', or -1 (got '$Value')."
	}

	if (($part -lt 0) -or ($part -gt 65534)) {
		throw "$Name must be 0 through 65534 (got $part)."
	}

	return $part
}

function Format-ExtensionVersion {
	param(
		[int] $Major,
		[int] $Minor,
		[int] $Build,
		[Nullable[int]] $Revision
	)

	$version = "$Major.$Minor.$Build"
	if ($null -ne $Revision) {
		$version = "$version.$Revision"
	}

	return $version
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
		Write-Host ("  {0,-40} {1} (unchanged)" -f $Label, $NewVersion)
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
$newBuild = Resolve-VersionPart -Value $Build -Name 'Build' -Auto ${function:Get-DayOfYearBuild}
$newRevision = Resolve-VersionPart -Value $Revision -Name 'Revision' -Auto ${function:Get-HalfSecondRevision} -AllowOmit

$newVersion = Format-ExtensionVersion -Major $newMajor -Minor $newMinor -Build $newBuild -Revision $newRevision

Write-Host ""
Write-Host "Cornerstone extension version bump"
Write-Host "  Current : $currentVersion"
Write-Host "  New     : $newVersion"
if ($WhatIf) {
	Write-Host "  Mode    : WhatIf (no files will be written)" -ForegroundColor Yellow
}
Write-Host ""
Write-Host "Targets:"

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
	Write-Host "Already at $newVersion - no files changed."
}
else {
	Write-Host "Updated $changed file(s) to $newVersion." -ForegroundColor Green
}
Write-Host ""
