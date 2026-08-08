<#
.SYNOPSIS
    Local validation for the Cornerstone VSIX + Marketplace publish profile.

.DESCRIPTION
    Microsoft does not ship a full "Marketplace accept/reject" offline validator.
    VsixPublisher only publishes; TF400898 errors are often opaque server-side.

    This script performs every practical local check we can:
      1. Package structure (zip, required files)
      2. VSIX Identity (upgrade-safe vs BobbyJCannon.Cornerstone)
      3. publishManifest.json rules (itemName, categories, overview)
      4. Asset paths in extension.vsixmanifest exist inside the VSIX
      5. XSD schema validation against VSSDK PackageManifest schemas (when found)
      6. Optional live comparison to the published store VSIX
      7. Optional local install dry-run via VSIXInstaller /quiet /admin /log

    Exit codes:
      0 = no errors (warnings allowed)
      1 = one or more errors

.EXAMPLE
    .\scripts\Validate-Extension.ps1

.EXAMPLE
    .\scripts\Validate-Extension.ps1 -SkipStoreCompare

.EXAMPLE
    .\scripts\Validate-Extension.ps1 -TryInstall
#>
param(
	[string] $Configuration = 'Release',
	[string] $Payload,
	[string] $ExpectedItemName = 'BobbyJCannon.Cornerstone',
	[string] $ExpectedVsixId = 'Cornerstone.Extension',
	[switch] $SkipStoreCompare,
	[switch] $TryInstall,
	[switch] $FailOnWarning
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$publishManifestPath = Join-Path $repoRoot 'Cornerstone.VisualStudio\publishManifest.json'
$defaultVsixPath = Join-Path $repoRoot "Cornerstone.VisualStudio\bin\$Configuration\net472\Cornerstone.VisualStudio.vsix"
$errors = New-Object System.Collections.Generic.List[string]
$warnings = New-Object System.Collections.Generic.List[string]

function Write-Check {
	param([string] $Status, [string] $Message)
	$color = switch ($Status) {
		'OK' { 'Green' }
		'WARN' { 'Yellow' }
		'FAIL' { 'Red' }
		default { 'Gray' }
	}
	Write-Host ("  [{0,-4}] {1}" -f $Status, $Message) -ForegroundColor $color
}

function Add-Error { param([string]$Message) $script:errors.Add($Message); Write-Check 'FAIL' $Message }
function Add-Warning { param([string]$Message) $script:warnings.Add($Message); Write-Check 'WARN' $Message }
function Add-Ok { param([string]$Message) Write-Check 'OK' $Message }

function Find-VsixPublisherBin {
	$candidates = @(
		'C:\Program Files\Microsoft Visual Studio\18\Professional\VSSDK\VisualStudioIntegration\Tools\Bin',
		'C:\Program Files\Microsoft Visual Studio\2022\Professional\VSSDK\VisualStudioIntegration\Tools\Bin'
	)
	$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
	if (Test-Path $vswhere) {
		$found = & $vswhere -latest -products * -find 'VSSDK\VisualStudioIntegration\Tools\Bin\VsixPublisher.exe' 2>$null
		foreach ($f in @($found)) {
			if ($f -and (Test-Path $f)) {
				return (Split-Path $f)
			}
		}
	}
	foreach ($c in $candidates) {
		if (Test-Path (Join-Path $c 'VsixPublisher.exe')) { return $c }
	}
	return $null
}

function Get-VsixIdentityFromXml {
	param([xml] $Xml)
	$id = $Xml.PackageManifest.Metadata.Identity
	$descNode = $Xml.PackageManifest.Metadata.Description
	$description = if ($descNode -is [System.Xml.XmlElement]) { $descNode.InnerText } else { [string]$descNode }
	return [pscustomobject]@{
		Id          = [string]$id.Id
		Version     = [string]$id.Version
		Publisher   = [string]$id.Publisher
		Language    = [string]$id.Language
		DisplayName = [string]$Xml.PackageManifest.Metadata.DisplayName
		Description = $description
		Icon        = [string]$Xml.PackageManifest.Metadata.Icon
		Tags        = [string]$Xml.PackageManifest.Metadata.Tags
	}
}

# --- Resolve payload ---
if ($Payload) {
	$vsixPath = if ([System.IO.Path]::IsPathRooted($Payload)) { $Payload } else { Join-Path (Get-Location) $Payload }
	$vsixPath = [System.IO.Path]::GetFullPath($vsixPath)
}
else {
	$vsixPath = $defaultVsixPath
}

Write-Host ""
Write-Host "Cornerstone VSIX local validation" -ForegroundColor White
Write-Host "  Payload : $vsixPath"
Write-Host ""

if (-not (Test-Path -LiteralPath $vsixPath)) {
	Add-Error "VSIX not found: $vsixPath"
	Write-Host ""
	Write-Host "Result: FAILED ($($errors.Count) error(s))" -ForegroundColor Red
	exit 1
}

$item = Get-Item -LiteralPath $vsixPath
Add-Ok ("File exists ({0:N1} KB, {1})" -f ($item.Length / 1KB), $item.LastWriteTime)

# --- Extract ---
Add-Type -AssemblyName System.IO.Compression.FileSystem
$extractRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("cornerstone-validate-" + [guid]::NewGuid().ToString('n'))
New-Item -ItemType Directory -Path $extractRoot | Out-Null
try {
	try {
		[System.IO.Compression.ZipFile]::ExtractToDirectory($vsixPath, $extractRoot)
		Add-Ok "VSIX is a valid zip and extracted"
	}
	catch {
		Add-Error "VSIX is not a valid zip package: $($_.Exception.Message)"
		throw
	}

	$manifestPath = Join-Path $extractRoot 'extension.vsixmanifest'
	if (-not (Test-Path $manifestPath)) {
		Add-Error "Missing extension.vsixmanifest inside VSIX"
	}
	else {
		Add-Ok "extension.vsixmanifest present"
	}

	foreach ($required in @('manifest.json', 'catalog.json')) {
		if (Test-Path (Join-Path $extractRoot $required)) {
			Add-Ok "$required present"
		}
		else {
			Add-Warning "$required missing (modern packages usually include it)"
		}
	}

	# --- Identity / metadata ---
	Write-Host ""
	Write-Host "Metadata" -ForegroundColor White
	[xml]$manifestXml = Get-Content -LiteralPath $manifestPath -Raw
	$identity = Get-VsixIdentityFromXml -Xml $manifestXml

	Write-Host "  Id          : $($identity.Id)"
	Write-Host "  Version     : $($identity.Version)"
	Write-Host "  Publisher   : $($identity.Publisher)"
	Write-Host "  DisplayName : $($identity.DisplayName)"
	Write-Host "  Description : $($identity.Description)"
	Write-Host "  Icon        : $($identity.Icon)"

	if ([string]::IsNullOrWhiteSpace($identity.DisplayName)) {
		Add-Error "DisplayName is empty (known cause of TF400898 - see microsoft/vsmarketplace#743)"
	}
	else {
		Add-Ok "DisplayName is non-empty"
	}

	if ([string]::IsNullOrWhiteSpace($identity.Description)) {
		Add-Error "Description is empty"
	}
	else {
		Add-Ok "Description is non-empty"
	}

	if ($identity.Id -cne $ExpectedVsixId) {
		Add-Error "VSIX Id '$($identity.Id)' != expected '$ExpectedVsixId' (client upgrades will break / new product)"
	}
	else {
		Add-Ok "VSIX Id matches expected upgrade id ($ExpectedVsixId)"
	}

	try {
		$null = [version]$identity.Version
		Add-Ok "Version parses as System.Version ($($identity.Version))"
	}
	catch {
		Add-Error "Version '$($identity.Version)' is not a valid System.Version"
	}

	if ($identity.Icon) {
		$iconPath = Join-Path $extractRoot ($identity.Icon -replace '/', '\')
		if (Test-Path -LiteralPath $iconPath) {
			Add-Ok "Icon file present in package ($($identity.Icon))"
		}
		else {
			Add-Error "Icon '$($identity.Icon)' referenced but missing from VSIX"
		}
	}
	else {
		Add-Warning "No Icon specified in vsixmanifest"
	}

	# --- Assets exist ---
	Write-Host ""
	Write-Host "Assets referenced by extension.vsixmanifest" -ForegroundColor White
	$assets = @($manifestXml.PackageManifest.Assets.Asset)
	if ($assets.Count -eq 0) {
		Add-Error "No Assets declared in extension.vsixmanifest"
	}
	foreach ($asset in $assets) {
		$path = [string]$asset.Path
		$type = [string]$asset.Type
		if ([string]::IsNullOrWhiteSpace($path)) {
			Add-Error "Asset type '$type' has empty Path"
			continue
		}
		# Skip MSBuild tokens if any leaked into a source-only manifest
		if ($path.Contains('%') -or $path.Contains('|')) {
			Add-Warning "Asset path still has MSBuild tokens (source manifest?): $path"
			continue
		}
		$full = Join-Path $extractRoot ($path -replace '/', '\')
		if (Test-Path -LiteralPath $full) {
			Add-Ok "$type -> $path"
		}
		else {
			Add-Error "Asset missing from VSIX: $type -> $path"
		}
	}

	# --- Installation targets ---
	Write-Host ""
	Write-Host "Installation targets" -ForegroundColor White
	$targets = @($manifestXml.PackageManifest.Installation.InstallationTarget)
	if ($targets.Count -eq 0) {
		Add-Error "No InstallationTarget entries"
	}
	foreach ($t in $targets) {
		$archNodes = @($t.ProductArchitecture)
		$arch = if ($archNodes.Count -gt 0) { ($archNodes | ForEach-Object { $_ }) -join ',' } else { '(none)' }
		Write-Host "  $($t.Id)  Version=$($t.Version)  Arch=$arch"
		if ($arch -eq '(none)') {
			Add-Warning "InstallationTarget $($t.Id) $($t.Version) has no ProductArchitecture (VS2022+ usually wants amd64/arm64)"
		}
		else {
			Add-Ok "InstallationTarget $($t.Id) $($t.Version) ($arch)"
		}
	}

	# --- publishManifest ---
	Write-Host ""
	Write-Host "Marketplace publishManifest" -ForegroundColor White
	if (-not (Test-Path -LiteralPath $publishManifestPath)) {
		Add-Error "Missing publishManifest.json at $publishManifestPath"
	}
	else {
		$pub = Get-Content -LiteralPath $publishManifestPath -Raw | ConvertFrom-Json
		$itemName = "$($pub.publisher).$($pub.identity.internalName)"
		Write-Host "  publisher    : $($pub.publisher)"
		Write-Host "  internalName : $($pub.identity.internalName)"
		Write-Host "  itemName     : $itemName"
		Write-Host "  categories   : $($pub.categories -join ', ')"
		Write-Host "  overview     : $($pub.overview)"

		if ($itemName -cne $ExpectedItemName) {
			Add-Error "itemName '$itemName' != expected '$ExpectedItemName' (would create/update a different listing)"
		}
		else {
			Add-Ok "itemName matches existing listing ($ExpectedItemName)"
		}

		if ($pub.identity.internalName -notmatch '^[A-Za-z0-9][A-Za-z0-9-]{0,62}$') {
			Add-Error "internalName '$($pub.identity.internalName)' invalid (A-Z a-z 0-9 - only, max 63, must start alphanumeric)"
		}
		else {
			Add-Ok "internalName charset/length OK"
		}

		$catCount = @($pub.categories).Count
		if ($catCount -lt 1 -or $catCount -gt 3) {
			Add-Error "categories must be 1-3 entries (found $catCount)"
		}
		else {
			Add-Ok "categories count OK ($catCount)"
		}

		$overviewFull = Join-Path (Split-Path $publishManifestPath) $pub.overview
		if (-not (Test-Path -LiteralPath $overviewFull)) {
			Add-Error "overview file missing: $overviewFull"
		}
		else {
			$overviewText = Get-Content -LiteralPath $overviewFull -Raw
			if ([string]::IsNullOrWhiteSpace($overviewText)) {
				Add-Error "overview.md is empty"
			}
			else {
				Add-Ok "overview.md present ($($overviewText.Length) chars)"
			}
		}
	}

	# --- XSD validation ---
	Write-Host ""
	Write-Host "XSD schema validation (VSSDK)" -ForegroundColor White
	$bin = Find-VsixPublisherBin
	if (-not $bin) {
		Add-Warning "VSSDK tools bin not found; skipping XSD validation"
	}
	else {
		$mainXsd = Join-Path $bin 'schemas\PackageManifestSchema.xsd'
		if (-not (Test-Path $mainXsd)) {
			Add-Warning "PackageManifestSchema.xsd not found under $bin\schemas"
		}
		else {
			try {
				# Load via XmlReader so BaseURI is the schema file path; that allows
				# relative xs:include (Metadata/Installation/Assets/...) to resolve.
				$resolver = New-Object System.Xml.XmlUrlResolver
				$schemaSet = New-Object System.Xml.Schema.XmlSchemaSet
				$schemaSet.XmlResolver = $resolver
				$schemaReaderSettings = New-Object System.Xml.XmlReaderSettings
				$schemaReaderSettings.XmlResolver = $resolver
				$schemaReader = [System.Xml.XmlReader]::Create($mainXsd, $schemaReaderSettings)
				try {
					# Must match schema targetNamespace; PowerShell $null can become '' and fail includes.
					$vsixNs = 'http://schemas.microsoft.com/developer/vsx-schema/2011'
					[void]$schemaSet.Add($vsixNs, $schemaReader)
					$schemaSet.Compile()
				}
				finally {
					$schemaReader.Close()
				}

				$script:XsdEventMessages = New-Object System.Collections.Generic.List[string]
				$settings = New-Object System.Xml.XmlReaderSettings
				$settings.ValidationType = [System.Xml.ValidationType]::Schema
				$settings.Schemas = $schemaSet
				$settings.XmlResolver = $resolver
				$settings.add_ValidationEventHandler({
						param($sender, $e)
						$script:XsdEventMessages.Add("$($e.Severity): $($e.Message)")
					})

				$reader = [System.Xml.XmlReader]::Create($manifestPath, $settings)
				try {
					while ($reader.Read()) { }
				}
				finally {
					$reader.Close()
				}

				if ($script:XsdEventMessages.Count -eq 0) {
					Add-Ok "PackageManifest XSD validation passed"
				}
				else {
					foreach ($xe in $script:XsdEventMessages) {
						if ($xe -like 'Warning:*') { Add-Warning "XSD $xe" } else { Add-Error "XSD $xe" }
					}
				}
			}
			catch {
				Add-Warning "XSD validation could not run: $($_.Exception.Message)"
			}
		}
	}

	# --- Live store compare ---
	if (-not $SkipStoreCompare) {
		Write-Host ""
		Write-Host "Live store comparison ($ExpectedItemName)" -ForegroundColor White
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
			if ($response.results -and $response.results[0].extensions) {
				$ext = $response.results[0].extensions | Select-Object -First 1
			}
			if (-not $ext) {
				Add-Error "Store listing '$ExpectedItemName' not found"
			}
			else {
				$latest = $ext.versions | Sort-Object { [version]$_.version } -Descending | Select-Object -First 1
				$storeVer = [string]$latest.version
				Add-Ok "Store listing found (latest version $storeVer)"

				try {
					if ([version]$identity.Version -gt [version]$storeVer) {
						Add-Ok "Local version $($identity.Version) > store $storeVer (upgrade path OK)"
					}
					elseif ([version]$identity.Version -eq [version]$storeVer) {
						Add-Warning "Local version equals store version $storeVer (republish same version may overwrite, but clients may not update)"
					}
					else {
						Add-Error "Local version $($identity.Version) <= store $storeVer"
					}
				}
				catch {
					Add-Warning "Could not compare versions: $_"
				}

				$pkg = $latest.files | Where-Object { $_.source -like '*.vsix' } | Select-Object -First 1
				if ($pkg) {
					$tmpStoreVsix = Join-Path ([System.IO.Path]::GetTempPath()) ("store-" + [guid]::NewGuid().ToString('n') + '.vsix')
					try {
						Invoke-WebRequest -Uri $pkg.source -OutFile $tmpStoreVsix
						$storeExtract = Join-Path ([System.IO.Path]::GetTempPath()) ("store-x-" + [guid]::NewGuid().ToString('n'))
						New-Item -ItemType Directory -Path $storeExtract | Out-Null
						try {
							[System.IO.Compression.ZipFile]::ExtractToDirectory($tmpStoreVsix, $storeExtract)
							[xml]$storeXml = Get-Content -LiteralPath (Join-Path $storeExtract 'extension.vsixmanifest') -Raw
							$storeId = Get-VsixIdentityFromXml -Xml $storeXml
							if ($storeId.Id -cne $identity.Id) {
								Add-Error "Store VSIX Id '$($storeId.Id)' != local '$($identity.Id)'"
							}
							else {
								Add-Ok "Store VSIX Id matches local ($($storeId.Id))"
							}
							if ($storeId.Publisher -cne $identity.Publisher) {
								Add-Warning "Store Publisher '$($storeId.Publisher)' != local '$($identity.Publisher)' (usually OK if only display formatting changed)"
							}
							else {
								Add-Ok "Store Publisher string matches local"
							}
							if ($storeId.DisplayName -cne $identity.DisplayName) {
								Add-Warning "DisplayName changed: store='$($storeId.DisplayName)' local='$($identity.DisplayName)'"
							}
						}
						finally {
							Remove-Item -LiteralPath $storeExtract -Recurse -Force -ErrorAction SilentlyContinue
						}
					}
					finally {
						Remove-Item -LiteralPath $tmpStoreVsix -Force -ErrorAction SilentlyContinue
					}
				}
			}
		}
		catch {
			Add-Warning "Store compare failed (network/API): $($_.Exception.Message)"
		}
	}

	# --- Optional install test ---
	if ($TryInstall) {
		Write-Host ""
		Write-Host "Local VSIXInstaller dry install" -ForegroundColor White
		$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
		$installer = $null
		if (Test-Path $vswhere) {
			$installer = & $vswhere -latest -products * -find 'Common7\IDE\VSIXInstaller.exe' 2>$null | Select-Object -First 1
		}
		if (-not $installer -or -not (Test-Path $installer)) {
			$installer = 'C:\Program Files\Microsoft Visual Studio\18\Professional\Common7\IDE\VSIXInstaller.exe'
		}
		if (-not (Test-Path $installer)) {
			Add-Warning "VSIXInstaller.exe not found; skip install test"
		}
		else {
			$log = Join-Path ([System.IO.Path]::GetTempPath()) ("vsixinstall-" + [guid]::NewGuid().ToString('n') + '.log')
			Write-Host "  Installer : $installer"
			Write-Host "  Log       : $log"
			# /quiet may still need elevation; /log captures details
			$args = @('/quiet', '/admin', "/logFile:$log", $vsixPath)
			$p = Start-Process -FilePath $installer -ArgumentList $args -Wait -PassThru
			if ($p.ExitCode -eq 0) {
				Add-Ok "VSIXInstaller exit code 0 (package accepted by local installer)"
			}
			else {
				Add-Warning "VSIXInstaller exit code $($p.ExitCode). See log: $log"
				if (Test-Path $log) {
					Get-Content $log -Tail 40 | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }
				}
			}
		}
	}
}
finally {
	Remove-Item -LiteralPath $extractRoot -Recurse -Force -ErrorAction SilentlyContinue
}

# --- Summary ---
Write-Host ""
Write-Host "Summary" -ForegroundColor White
Write-Host "  Errors   : $($errors.Count)"
Write-Host "  Warnings : $($warnings.Count)"
Write-Host ""
Write-Host "Notes:" -ForegroundColor DarkGray
Write-Host "  - There is no public offline 'Marketplace will accept this' API." -ForegroundColor DarkGray
Write-Host "  - VsixPublisher has no validate command; publish is the only server check." -ForegroundColor DarkGray
Write-Host "  - Local pass still cannot rule out TF400898 server/auth bugs." -ForegroundColor DarkGray
Write-Host "  - Canary: re-upload the currently published 1.0.1 VSIX; if that fails too, it is not your 1.2.x content." -ForegroundColor DarkGray
Write-Host ""

if ($errors.Count -gt 0 -or ($FailOnWarning -and $warnings.Count -gt 0)) {
	Write-Host "Result: FAILED" -ForegroundColor Red
	exit 1
}

Write-Host "Result: PASSED (local checks)" -ForegroundColor Green
exit 0
