# Compiles remotelink-server.jar as DEX (ART/app_process). Used by Cornerstone.RemoteLink.csproj.
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$src = Join-Path $root "src\main\java"
$classes = Join-Path $root "build\classes"
$dexDir = Join-Path $root "build\dex"
$libs = Join-Path $root "build\libs"
$jarPath = Join-Path $libs "remotelink-server.jar"
$manifest = Join-Path $root "build\remotelink-server.manifest"

function Resolve-JavaTool([string]$name) {
	$exe = "$name.exe"
	if ($env:JAVA_HOME) {
		$candidate = Join-Path $env:JAVA_HOME "bin\$exe"
		if (Test-Path $candidate) {
			return $candidate
		}
	}

	$cmd = Get-Command $name -ErrorAction SilentlyContinue
	if ($cmd -ne $null) {
		return $cmd.Source
	}

	$searchRoots = @(
		"$env:ProgramFiles\Android\Android Studio\jbr",
		"$env:ProgramFiles\Android\Android Studio\jre",
		"${env:ProgramFiles(x86)}\Android\android-studio\jbr",
		"$env:ProgramFiles\Android\jdk",
		"$env:ProgramFiles\Android\openjdk",
		"$env:ProgramFiles\Eclipse Adoptium",
		"$env:ProgramFiles\Microsoft",
		"$env:ProgramFiles\Java",
		"$env:LOCALAPPDATA\Programs\Eclipse Adoptium"
	)
	foreach ($rootPath in $searchRoots) {
		if (-not (Test-Path $rootPath)) {
			continue
		}
		$direct = Join-Path $rootPath "bin\$exe"
		if (Test-Path $direct) {
			return $direct
		}
		$nested = Get-ChildItem -Path $rootPath -Filter $exe -Recurse -ErrorAction SilentlyContinue |
			Where-Object { $_.Directory.Name -eq "bin" } |
			Select-Object -First 1
		if ($nested -ne $null) {
			return $nested.FullName
		}
	}

	throw "JDK 17+ is required to build remotelink-server.jar. Set JAVA_HOME or install a JDK (Android Studio's jbr is enough)."
}

function Resolve-AndroidJar {
	$sdkRoots = @(
		$env:ANDROID_HOME,
		$env:ANDROID_SDK_ROOT,
		"$env:LOCALAPPDATA\Android\Sdk",
		"${env:ProgramFiles(x86)}\Android\android-sdk",
		"$env:ProgramFiles\Android\android-sdk"
	)
	foreach ($sdk in $sdkRoots) {
		if ([string]::IsNullOrWhiteSpace($sdk)) {
			continue
		}
		$platforms = Join-Path $sdk "platforms"
		if (-not (Test-Path $platforms)) {
			continue
		}
		$jarFile = Get-ChildItem $platforms -Directory |
			Sort-Object { $_.Name } -Descending |
			ForEach-Object { Join-Path $_.FullName "android.jar" } |
			Where-Object { Test-Path $_ } |
			Select-Object -First 1
		if ($jarFile -ne $null) {
			return $jarFile
		}
	}

	return $null
}

function Resolve-D8 {
	$sdkRoots = @(
		$env:ANDROID_HOME,
		$env:ANDROID_SDK_ROOT,
		"$env:LOCALAPPDATA\Android\Sdk",
		"${env:ProgramFiles(x86)}\Android\android-sdk",
		"$env:ProgramFiles\Android\android-sdk"
	)
	foreach ($sdk in $sdkRoots) {
		if ([string]::IsNullOrWhiteSpace($sdk)) {
			continue
		}
		$tools = Join-Path $sdk "build-tools"
		if (-not (Test-Path $tools)) {
			continue
		}
		$d8 = Get-ChildItem $tools -Directory |
			Sort-Object { $_.Name } -Descending |
			ForEach-Object { Join-Path $_.FullName "d8.bat" } |
			Where-Object { Test-Path $_ } |
			Select-Object -First 1
		if ($d8 -ne $null) {
			return $d8
		}
	}

	$cmd = Get-Command "d8" -ErrorAction SilentlyContinue
	if ($cmd -ne $null) {
		return $cmd.Source
	}

	throw "Android SDK build-tools d8 is required so the jar contains classes.dex (ART aborts a desktop .class jar). Set ANDROID_HOME or install platform build-tools."
}

$javac = Resolve-JavaTool "javac"
$jar = Resolve-JavaTool "jar"
$d8 = Resolve-D8
$javaHome = [System.IO.Path]::GetFullPath((Join-Path ([System.IO.Path]::GetDirectoryName($javac)) ".."))
$env:JAVA_HOME = $javaHome
$env:Path = (Join-Path $javaHome "bin") + ";" + $env:Path
Write-Host "JAVA_HOME=$javaHome"
Write-Host "d8=$d8"
New-Item -ItemType Directory -Force -Path $classes | Out-Null
New-Item -ItemType Directory -Force -Path $dexDir | Out-Null
New-Item -ItemType Directory -Force -Path $libs | Out-Null
$sources = Get-ChildItem -Path $src -Filter "*.java" -Recurse | ForEach-Object { $_.FullName }
if ($sources.Count -eq 0) {
	throw "No Java sources under $src."
}

& $javac -encoding UTF-8 --release 11 -d $classes @sources
if ($LASTEXITCODE -ne 0) {
	throw "javac failed with exit code $LASTEXITCODE."
}

$classFiles = Get-ChildItem -Path $classes -Filter "*.class" -Recurse | ForEach-Object { $_.FullName }
if ($classFiles.Count -eq 0) {
	throw "javac produced no .class files."
}

$androidJar = Resolve-AndroidJar
$d8Args = @("--min-api", "21", "--output", $dexDir)
if ($androidJar -ne $null) {
	Write-Host "android.jar=$androidJar"
	$d8Args += @("--lib", $androidJar)
}
$d8Args += $classFiles
& $d8 @d8Args
if ($LASTEXITCODE -ne 0) {
	throw "d8 failed with exit code $LASTEXITCODE."
}

$dex = Join-Path $dexDir "classes.dex"
if (-not (Test-Path $dex)) {
	throw "d8 did not write $dex."
}

@(
	"Manifest-Version: 1.0"
	"Main-Class: cornerstone.remotelink.server.RemoteLinkServer"
	"Implementation-Title: Cornerstone RemoteLink Android server"
) | Set-Content -Path $manifest -Encoding ascii
if (Test-Path $jarPath) {
	Remove-Item $jarPath -Force
}
& $jar cfm $jarPath $manifest -C $dexDir classes.dex
if ($LASTEXITCODE -ne 0) {
	throw "jar failed with exit code $LASTEXITCODE."
}

Write-Host "Built $jarPath (DEX via $d8)"
