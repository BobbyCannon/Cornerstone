<#
.SYNOPSIS
    Build and/or publish the Cornerstone VSIX to the Visual Studio Marketplace.

.DESCRIPTION
    Uses the existing Marketplace publish profile:
      Cornerstone.VisualStudio/publishManifest.json
      Cornerstone.VisualStudio/overview.md

    Auth (first match wins):
      1. -PersonalAccessToken parameter
      2. $env:VS_MARKETPLACE_PAT
      3. Cached login from a prior:  .\scripts\Publish-Extension.ps1 -Login

    Create a Marketplace PAT at:
      https://marketplace.visualstudio.com/manage/publishers/
    (All accessible organizations -> Custom -> Marketplace: Manage)

.EXAMPLE
    # One-time login (stores PAT for this publisher on the machine)
    .\scripts\Publish-Extension.ps1 -Login -PersonalAccessToken $pat

.EXAMPLE
    # Build Release VSIX only (no Marketplace upload)
    .\scripts\Publish-Extension.ps1 -SkipPublish

.EXAMPLE
    # Build + publish using env PAT or cached login
    $env:VS_MARKETPLACE_PAT = '...'
    .\scripts\Publish-Extension.ps1

.EXAMPLE
    # Publish an already-built VSIX without rebuilding
    .\scripts\Publish-Extension.ps1 -SkipBuild -Payload .\artifacts\Cornerstone.VisualStudio.vsix
#>
param(
	[string] $Configuration = 'Release',
	[string] $Payload,
	[string] $PersonalAccessToken = $env:VS_MARKETPLACE_PAT,
	[string] $PublisherName,
	[string] $IgnoreWarnings = '',
	[switch] $SkipBuild,
	[switch] $SkipPublish,
	[switch] $VerifyOnly,
	[switch] $Login,
	[switch] $Logout,
	[switch] $WhatIf
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projectPath = Join-Path $repoRoot 'Cornerstone.VisualStudio\Cornerstone.VisualStudio.csproj'
$publishManifestPath = Join-Path $repoRoot 'Cornerstone.VisualStudio\publishManifest.json'
$overviewPath = Join-Path $repoRoot 'Cornerstone.VisualStudio\overview.md'
$defaultVsixPath = Join-Path $repoRoot "Cornerstone.VisualStudio\bin\$Configuration\net472\Cornerstone.VisualStudio.vsix"

function Get-PublisherName {
	param([string] $Override)
	if ($Override) {
		return $Override
	}
	if (-not (Test-Path -LiteralPath $publishManifestPath)) {
		throw "Missing publish profile: $publishManifestPath"
	}
	$manifest = Get-Content -LiteralPath $publishManifestPath -Raw | ConvertFrom-Json
	if (-not $manifest.publisher) {
		throw "publishManifest.json is missing required 'publisher' field."
	}
	return [string]$manifest.publisher
}

function Find-VsixPublisher {
	$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
	if (-not (Test-Path -LiteralPath $vswhere)) {
		throw "vswhere.exe not found at $vswhere. Install Visual Studio with the VSSDK workload."
	}

	$found = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VSSDK `
		-find 'VSSDK\VisualStudioIntegration\Tools\Bin\VsixPublisher.exe' 2>$null
	if ($found) {
		$first = @($found | Where-Object { $_ -and (Test-Path -LiteralPath $_) }) | Select-Object -First 1
		if ($first) {
			return $first
		}
	}

	# Fallback: any recent VS install that has VsixPublisher
	$found = & $vswhere -latest -products * -find 'VSSDK\VisualStudioIntegration\Tools\Bin\VsixPublisher.exe' 2>$null
	$first = @($found | Where-Object { $_ -and (Test-Path -LiteralPath $_) }) | Select-Object -First 1
	if ($first) {
		return $first
	}

	throw 'VsixPublisher.exe not found. Install the Visual Studio extension development workload (VSSDK).'
}

function Get-RedactedArgumentList {
	param([string[]] $ArgumentList)
	$redacted = New-Object System.Collections.Generic.List[string]
	for ($i = 0; $i -lt $ArgumentList.Count; $i++) {
		$arg = $ArgumentList[$i]
		$redacted.Add($arg)
		# Never echo the token value after -personalAccessToken
		if ($arg -ieq '-personalAccessToken' -and ($i + 1) -lt $ArgumentList.Count) {
			$redacted.Add('***')
			$i++
		}
	}
	return $redacted.ToArray()
}

function Invoke-VsixPublisher {
	param(
		[string] $Exe,
		[string[]] $ArgumentList
	)
	$displayArgs = Get-RedactedArgumentList -ArgumentList $ArgumentList
	Write-Host ""
	Write-Host "VsixPublisher $($displayArgs -join ' ')" -ForegroundColor DarkGray
	if ($WhatIf) {
		Write-Host "WhatIf: skipping VsixPublisher invocation." -ForegroundColor Yellow
		return
	}
	& $Exe @ArgumentList
	if ($LASTEXITCODE -ne 0) {
		throw "VsixPublisher failed with exit code $LASTEXITCODE."
	}
}

function Get-VsixIdentity {
	param([string] $VsixPath)
	Add-Type -AssemblyName System.IO.Compression.FileSystem
	$zip = [System.IO.Compression.ZipFile]::OpenRead($VsixPath)
	try {
		$entry = $zip.Entries | Where-Object { $_.FullName -eq 'extension.vsixmanifest' } | Select-Object -First 1
		if (-not $entry) {
			throw "VSIX is missing extension.vsixmanifest: $VsixPath"
		}
		$reader = New-Object System.IO.StreamReader($entry.Open())
		try {
			$xml = [xml]$reader.ReadToEnd()
		}
		finally {
			$reader.Close()
		}
		$id = $xml.PackageManifest.Metadata.Identity
		return [pscustomobject]@{
			Id        = [string]$id.Id
			Version   = [string]$id.Version
			Publisher = [string]$id.Publisher
			Language  = [string]$id.Language
		}
	}
	finally {
		$zip.Dispose()
	}
}

function Assert-SafeMarketplaceUpgrade {
	<#
		Hard gate: refuse to publish unless this package updates the EXISTING listing
		BobbyJCannon.Cornerstone (VSIX Id Cornerstone.Extension), never a new itemName.
	#>
	param(
		[string] $PublishManifestPath,
		[string] $VsixPath,
		[string] $ExpectedItemName = 'BobbyJCannon.Cornerstone',
		[string] $ExpectedVsixId = 'Cornerstone.Extension'
	)

	Write-Host "Preflight: verifying this upload updates existing listing only..." -ForegroundColor Cyan

	$pub = Get-Content -LiteralPath $PublishManifestPath -Raw | ConvertFrom-Json
	$internalName = [string]$pub.identity.internalName
	$pubName = [string]$pub.publisher
	$itemName = "$pubName.$internalName"

	$failures = New-Object System.Collections.Generic.List[string]

	if ($internalName -notmatch '^[A-Za-z0-9][A-Za-z0-9-]{0,62}$') {
		$failures.Add("publishManifest internalName '$internalName' is invalid (Marketplace allows A-Z a-z 0-9 - only, max 63 chars).")
	}
	if ($itemName -cne $ExpectedItemName) {
		$failures.Add("Computed itemName is '$itemName' but must be exactly '$ExpectedItemName' to update the existing listing.")
	}

	$identity = Get-VsixIdentity -VsixPath $VsixPath
	if ($identity.Id -cne $ExpectedVsixId) {
		$failures.Add("VSIX Identity Id is '$($identity.Id)' but installed clients upgrade only if it stays '$ExpectedVsixId'.")
	}

	# Live gallery check (must already exist - we never want a brand-new listing accidentally)
	$galleryOk = $false
	$storeVersion = $null
	$storeVsixId = $null
	try {
		$body = @{
			filters    = @(@{
					criteria   = @(@{ filterType = 7; value = $ExpectedItemName })
					pageNumber = 1
					pageSize   = 1
					sortBy     = 0
					sortOrder  = 0
				})
			assetTypes = @()
			flags      = 914
		} | ConvertTo-Json -Depth 6

		$response = Invoke-RestMethod -Method Post `
			-Uri 'https://marketplace.visualstudio.com/_apis/public/gallery/extensionquery?api-version=7.1-preview.1' `
			-ContentType 'application/json' `
			-Body $body

		$ext = $null
		if ($response.results -and $response.results.Count -gt 0 -and $response.results[0].extensions) {
			$ext = $response.results[0].extensions | Select-Object -First 1
		}
		if (-not $ext) {
			$failures.Add("Gallery query found no extension for '$ExpectedItemName'. Aborting to avoid creating a new listing.")
		}
		else {
			$galleryOk = $true
			$liveItem = "$($ext.publisher.publisherName).$($ext.extensionName)"
			if ($liveItem -cne $ExpectedItemName) {
				$failures.Add("Gallery returned '$liveItem' instead of '$ExpectedItemName'.")
			}
			$latest = $ext.versions | Sort-Object { [version]$_.version } -Descending | Select-Object -First 1
			$storeVersion = [string]$latest.version

			# Confirm VSIX Id from live package when CDN URL is available
			$pkg = $latest.files | Where-Object { $_.source -like '*.vsix' } | Select-Object -First 1
			if ($pkg) {
				$tmp = Join-Path ([System.IO.Path]::GetTempPath()) ("cornerstone-preflight-{0}.vsix" -f [guid]::NewGuid().ToString('n'))
				try {
					Invoke-WebRequest -Uri $pkg.source -OutFile $tmp
					$storeIdentity = Get-VsixIdentity -VsixPath $tmp
					$storeVsixId = $storeIdentity.Id
					if ($storeVsixId -cne $ExpectedVsixId) {
						$failures.Add("Live store VSIX Id is '$storeVsixId' but local expects '$ExpectedVsixId'.")
					}
					if ($identity.Id -cne $storeVsixId) {
						$failures.Add("Local VSIX Id '$($identity.Id)' does not match live store VSIX Id '$storeVsixId'.")
					}
				}
				finally {
					Remove-Item -LiteralPath $tmp -Force -ErrorAction SilentlyContinue
				}
			}

			try {
				$localVer = [version]$identity.Version
				$remoteVer = [version]$storeVersion
				if ($localVer -le $remoteVer) {
					$failures.Add("Local version $($identity.Version) is not greater than store version $storeVersion. Bump version before publishing.")
				}
			}
			catch {
				$failures.Add("Could not compare versions (local='$($identity.Version)', store='$storeVersion'): $_")
			}
		}
	}
	catch {
		$failures.Add("Gallery preflight failed (network/API). Refusing to publish without confirming existing listing: $($_.Exception.Message)")
	}

	Write-Host "  Expected listing : $ExpectedItemName"
	Write-Host "  publishManifest  : $itemName"
	Write-Host "  Local VSIX Id    : $($identity.Id)"
	Write-Host "  Local version    : $($identity.Version)"
	if ($galleryOk) {
		Write-Host "  Live listing     : FOUND"
		Write-Host "  Live version     : $storeVersion"
		if ($storeVsixId) {
			Write-Host "  Live VSIX Id     : $storeVsixId"
		}
	}

	if ($failures.Count -gt 0) {
		Write-Host ""
		Write-Host "PREFLIGHT FAILED - publish aborted (will not upload)." -ForegroundColor Red
		foreach ($f in $failures) {
			Write-Host "  - $f" -ForegroundColor Red
		}
		throw "Marketplace preflight failed with $($failures.Count) issue(s). Fix identities before publishing."
	}

	Write-Host "  Preflight        : PASS - will UPDATE existing listing only." -ForegroundColor Green
}

$publisher = Get-PublisherName -Override $PublisherName
$vsixPublisher = Find-VsixPublisher

Write-Host ""
Write-Host "Cornerstone Marketplace publish" -ForegroundColor White
Write-Host "  Publisher : $publisher"
Write-Host "  Profile   : $publishManifestPath"
Write-Host "  Tool      : $vsixPublisher"
if ($WhatIf) {
	Write-Host "  Mode      : WhatIf" -ForegroundColor Yellow
}
Write-Host ""

if (-not (Test-Path -LiteralPath $publishManifestPath)) {
	throw "Missing publish profile: $publishManifestPath"
}
if (-not (Test-Path -LiteralPath $overviewPath)) {
	throw "Missing overview.md (Marketplace readme): $overviewPath"
}

# --- Login / Logout ---
if ($Logout) {
	Invoke-VsixPublisher -Exe $vsixPublisher -ArgumentList @(
		'logout',
		'-publisherName', $publisher,
		'-ignoreMissingPublisher'
	)
	Write-Host "Logged out publisher '$publisher'." -ForegroundColor Green
	if (-not $Login -and $SkipPublish -and $SkipBuild) {
		return
	}
}

if ($Login) {
	if (-not $PersonalAccessToken) {
		throw "Login requires -PersonalAccessToken or `$env:VS_MARKETPLACE_PAT."
	}
	Invoke-VsixPublisher -Exe $vsixPublisher -ArgumentList @(
		'login',
		'-publisherName', $publisher,
		'-personalAccessToken', $PersonalAccessToken,
		'-overwrite'
	)
	Write-Host "Logged in publisher '$publisher'." -ForegroundColor Green
	if ($SkipPublish -and $SkipBuild) {
		return
	}
}

# --- Build ---
if (-not $SkipBuild) {
	if (-not (Test-Path -LiteralPath $projectPath)) {
		throw "Missing project: $projectPath"
	}
	Write-Host "Building $Configuration VSIX..." -ForegroundColor Cyan
	if ($WhatIf) {
		Write-Host "WhatIf: would run dotnet build `"$projectPath`" -c $Configuration" -ForegroundColor Yellow
	}
	else {
		#dotnet build $projectPath -c $Configuration --nologo
		$msbuild = "C:\Program Files\Microsoft Visual Studio\18\Professional\MSBuild\Current\Bin\MSBuild.exe"
		& $msbuild "C:\Workspaces\EpicSolution\Cornerstone.VisualStudio\Cornerstone.VisualStudio\Cornerstone.VisualStudio.csproj" /p:Configuration=Release

		if ($LASTEXITCODE -ne 0) {
			throw "dotnet build failed with exit code $LASTEXITCODE."
		}
	}
}

# --- Resolve payload ---
if ($Payload) {
	$vsixPath = $Payload
	if (-not [System.IO.Path]::IsPathRooted($vsixPath)) {
		$vsixPath = Join-Path (Get-Location) $vsixPath
	}
	$vsixPath = [System.IO.Path]::GetFullPath($vsixPath)
}
else {
	$vsixPath = $defaultVsixPath
}

if (-not $WhatIf -and -not (Test-Path -LiteralPath $vsixPath)) {
	throw "VSIX not found at $vsixPath. Build first or pass -Payload."
}

Write-Host "Payload    : $vsixPath" -ForegroundColor Cyan
if (-not $WhatIf -and (Test-Path -LiteralPath $vsixPath)) {
	$item = Get-Item -LiteralPath $vsixPath
	Write-Host ("Size       : {0:N1} KB  ({1})" -f ($item.Length / 1KB), $item.LastWriteTime)
}

# Hard gate: never upload if this would create a different Marketplace itemName
# or break client upgrades for the existing BobbyJCannon.Cornerstone listing.
if (-not $WhatIf) {
	Assert-SafeMarketplaceUpgrade -PublishManifestPath $publishManifestPath -VsixPath $vsixPath
}
else {
	Write-Host "WhatIf: would run marketplace identity preflight against BobbyJCannon.Cornerstone" -ForegroundColor Yellow
}

if ($VerifyOnly -or $SkipPublish) {
	$reason = if ($VerifyOnly) { 'VerifyOnly' } else { 'SkipPublish' }
	Write-Host "$reason : identity preflight complete; no Marketplace upload." -ForegroundColor DarkGray
	return
}

# --- Publish ---
$publishArgs = @(
	'publish',
	'-payload', $vsixPath,
	'-publishManifest', $publishManifestPath
)

if ($PersonalAccessToken) {
	$publishArgs += @('-personalAccessToken', $PersonalAccessToken)
}

if ($IgnoreWarnings) {
	$publishArgs += @('-ignoreWarnings', $IgnoreWarnings)
}

Write-Host "Publishing to Visual Studio Marketplace..." -ForegroundColor Cyan
Invoke-VsixPublisher -Exe $vsixPublisher -ArgumentList $publishArgs

Write-Host ""
Write-Host "Publish complete." -ForegroundColor Green
Write-Host "  Manage: https://marketplace.visualstudio.com/manage/publishers/$([uri]::EscapeDataString($publisher))"
Write-Host "  Listing: https://marketplace.visualstudio.com/items?itemName=BobbyJCannon.Cornerstone"
Write-Host ""
