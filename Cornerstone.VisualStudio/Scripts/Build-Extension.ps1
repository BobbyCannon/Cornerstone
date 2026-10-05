<#
.SYNOPSIS
    Stamps the Cornerstone VSIX version, rebuilds Release, and copies Cornerstone-v{version}.vsix.

.DESCRIPTION
    Writes the clock-based VSIX version, converts templates at -CornerstoneVersion,
    rebuilds the extension, and copies the VSIX to builds\Cornerstone-v{version}.vsix.
    Prints the VSIX version on the output stream so a caller can capture it.

    -CornerstoneVersion is the NuGet version the templates reference. It is not the VSIX version.
    Pass the staged version from Build-Cornerstone.NuGet.ps1. The VSIX pack forwards it so the
    embedded Cornerstone.Templates package matches that version.

.PARAMETER CornerstoneVersion
    NuGet version of the Cornerstone packages, such as 3.0.275.29002.
#>
param(
	[Parameter(Mandatory = $true)]
	[string] $CornerstoneVersion
)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
Set-Location $repo

if ($CornerstoneVersion -notmatch '^\d+\.\d+\.\d+\.\d+$') {
	throw "CornerstoneVersion must be Major.Minor.Build.Revision. Got '$CornerstoneVersion'."
}

$root = Join-Path $repo 'Cornerstone.VisualStudio'
$proj = Join-Path $root 'Cornerstone.VisualStudio\Cornerstone.VisualStudio.csproj'
$manifest = Join-Path $root 'Cornerstone.VisualStudio\source.extension.vsixmanifest'
$vsix = Join-Path $root 'Cornerstone.VisualStudio\bin\Release\net472\Cornerstone.VisualStudio.vsix'

# 1) Version (prefer not hardcoding Build every time)
& (Join-Path $root 'Scripts\Update-ExtensionVersion.ps1') -Major 1 -Minor 5 -Build * -Revision *

$manifestText = Get-Content -LiteralPath $manifest -Raw
$versionMatch = [regex]::Match($manifestText, 'Identity\b[^>]*\bVersion="([^"]+)"')
if (-not $versionMatch.Success) {
	throw "Could not read Identity Version from $manifest"
}

$version = $versionMatch.Groups[1].Value

# 1.5) Stamp template PackageReferences, then rebuild. The VSIX pack converts again
# with CornerstonePackageVersion so the embedded nupkg uses this same version.
Write-Host "Embedding Cornerstone templates $CornerstoneVersion" -ForegroundColor Cyan
& (Join-Path $repo 'Templates\Convert-Templates.ps1') -Version $CornerstoneVersion

# 2) Clean + restore + rebuild
$msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
	-latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' |
	Select-Object -First 1
if (-not $msbuild) {
	throw "MSBuild was not found."
}

Remove-Item (Join-Path $root 'Cornerstone.VisualStudio\bin'), (Join-Path $root 'Cornerstone.VisualStudio\obj') -Recurse -ErrorAction Ignore
& $msbuild $proj /t:Restore /p:Configuration=Release /p:CornerstonePackageVersion=$CornerstoneVersion | Out-Host
if ($LASTEXITCODE -ne 0) { throw "Restore failed: $LASTEXITCODE" }
& $msbuild $proj /t:Rebuild /p:Configuration=Release /p:CornerstonePackageVersion=$CornerstoneVersion | Out-Host
if ($LASTEXITCODE -ne 0) { throw "Rebuild failed: $LASTEXITCODE" }
if (-not (Test-Path $vsix)) { throw "VSIX missing: $vsix" }

# 3) Local install payload
$destinationDir = Join-Path $repo 'builds'
$destination = Join-Path $destinationDir "Cornerstone-v$version.vsix"
New-Item -ItemType Directory -Path $destinationDir -Force | Out-Null
Copy-Item $vsix $destination -Force
Write-Host "Copied $destination" -ForegroundColor Green
Write-Output $version
