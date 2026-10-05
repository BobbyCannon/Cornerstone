param(
	[string] $AdbPath,
	[string] $AdbTarget
)

$ErrorActionPreference = "Stop"

function Get-AdbPath {
	param([string] $Preferred)
	if ($Preferred -and (Test-Path -LiteralPath $Preferred)) {
		return (Resolve-Path -LiteralPath $Preferred).Path
	}
	$candidates = @(
		"${env:ProgramFiles(x86)}\Android\android-sdk\platform-tools\adb.exe",
		"$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
	)
	foreach ($candidate in $candidates) {
		if (Test-Path -LiteralPath $candidate) {
			return $candidate
		}
	}
	throw "adb.exe not found. Install the Android SDK platform-tools used by Visual Studio."
}

function Get-AdbArgumentList {
	param(
		[string] $Target,
		[string[]] $Rest
	)
	$list = @()
	if ($Target) {
		$list += @($Target.Trim() -split '\s+' | Where-Object { $_ })
	}
	$list += $Rest
	return $list
}

function Get-AdbState {
	param(
		[string] $Exe,
		[string] $Target
	)
	$raw = & $Exe @(Get-AdbArgumentList -Target $Target -Rest @('get-state')) 2>$null | Out-String
	return $raw.Trim()
}

function Wait-AdbDevice {
	param(
		[string] $Exe,
		[string] $Target,
		[int] $Seconds = 20
	)
	& $Exe @(Get-AdbArgumentList -Target $Target -Rest @('wait-for-device'))
	$deadline = (Get-Date).AddSeconds($Seconds)
	$state = ""
	do {
		$state = Get-AdbState -Exe $Exe -Target $Target
		if ($state -eq "device") {
			return $state
		}
		Start-Sleep -Milliseconds 500
	} while ((Get-Date) -lt $deadline)
	return $state
}

$AdbPath = Get-AdbPath -Preferred $AdbPath
$expected = [IO.Path]::GetFullPath($AdbPath)
$processes = @(Get-Process -Name adb -ErrorAction SilentlyContinue)
$foreign = @()
foreach ($process in $processes) {
	# Path is often empty without elevation; that is not proof of a second server.
	if ($process.Path -and ([IO.Path]::GetFullPath($process.Path) -ne $expected)) {
		$foreign += $process
	}
}

if ($foreign.Count -gt 0) {
	Write-Host "Stopping $($foreign.Count) adb.exe that is not Visual Studio's ($AdbPath)."
	$foreign | Stop-Process -Force -ErrorAction SilentlyContinue
	Start-Sleep -Milliseconds 400
	& $AdbPath kill-server 2>$null | Out-Null
	& $AdbPath start-server | Out-Null
}

# Software unplug: kick the USB/tcp session so jdwp is not stuck. Do this before
# VS starts MonoVsDbg. Do not adb forward --remove-all here — VS may already be
# listening on 8895 and a second debugger process will fight the first.
Write-Host "Reconnecting adb device (same effect as unplug for a hung jdwp session)."
& $AdbPath @(Get-AdbArgumentList -Target $AdbTarget -Rest @('reconnect')) 2>$null | Out-Null
Start-Sleep -Milliseconds 600

$state = Wait-AdbDevice -Exe $AdbPath -Target $AdbTarget
if ($state -ne "device") {
	throw "adb is '$state' after reconnect. Unlock the phone, accept USB debugging, then F5 again. scrcpy must use this adb: $AdbPath"
}

Write-Host "adb ready: $AdbPath"
