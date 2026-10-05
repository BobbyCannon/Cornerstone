<#
.SYNOPSIS
	Copies live template projects under Cornerstone/Templates into Cornerstone.Templates/content
	and rewrites ProjectReferences to NuGet PackageReferences.

.DESCRIPTION
	Source of truth is Cornerstone/Templates. Generated output is packed by Cornerstone.Templates
	and embedded in the VSIX. Run this before pack, or let Cornerstone.Templates.csproj invoke it.

	-Version sets the default Cornerstone NuGet version in template.json (the value
	dotnet new uses for CornerstoneVersionTemplateParameter). When omitted, Version
	is read from Cornerstone/Directory.Build.props.
#>
[CmdletBinding()]
param (
	[string] $TemplatesRoot = $PSScriptRoot,
	[string] $ContentRoot,
	[string] $Version
)

$ErrorActionPreference = "Stop"

$cornerstoneRoot = Split-Path $TemplatesRoot -Parent
if ([string]::IsNullOrWhiteSpace($ContentRoot)) {
	$ContentRoot = Join-Path $cornerstoneRoot "Cornerstone.Templates\content"
}

$cornerstoneVersion = $Version
if ([string]::IsNullOrWhiteSpace($cornerstoneVersion)) {
	$directoryBuildProps = Join-Path $cornerstoneRoot "Directory.Build.props"
	[xml] $versionXml = Get-Content $directoryBuildProps
	$cornerstoneVersion = ($versionXml.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version
	if ([string]::IsNullOrWhiteSpace($cornerstoneVersion)) {
		throw "Could not read Version from $directoryBuildProps. Pass -Version."
	}
}

$packageVersionToken = "CornerstoneVersionTemplateParameter"

# Each subdirectory under Templates/ is a template; folder name is the content id.
$templates = @(Get-ChildItem $TemplatesRoot -Directory | ForEach-Object { @{ Id = $_.Name } })
if ($templates.Count -eq 0) {
	throw "No template folders found under $TemplatesRoot"
}

$packageByProjectFile = @{
	"Cornerstone.csproj"                       = "Cornerstone"
	"Cornerstone.Presentation.csproj"           = "Cornerstone.Presentation"
	"Cornerstone.EntityFramework.csproj"        = "Cornerstone.EntityFramework"
	"Cornerstone.Automation.csproj"             = "Cornerstone.Automation"
	"Cornerstone.Automation.Presentation.csproj" = "Cornerstone.Automation.Presentation"
}

$dropProjectFiles = @(
	"Cornerstone.Presentation.BuildTasks.csproj",
	"Cornerstone.Generators.csproj"
)

function Test-ShouldSkipPath {
	param ([string] $RelativePath)

	$parts = $RelativePath -split '[\\/]'
	if ($parts -contains "bin" -or $parts -contains "obj" -or $parts -contains ".vs") {
		return $true
	}
	$name = Split-Path $RelativePath -Leaf
	if ($name -eq "Directory.Build.props" -or $name -eq "Directory.Build.targets") {
		return $true
	}
	if ($name -like "*.user") {
		return $true
	}
	return $false
}

function Convert-ProjectFile {
	param ([string] $ProjectPath)

	[xml] $xml = Get-Content $ProjectPath
	$nsmgr = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
	$nsmgr.AddNamespace("ms", "http://schemas.microsoft.com/developer/msbuild/2003")
	$hasNs = $xml.DocumentElement.NamespaceURI -eq "http://schemas.microsoft.com/developer/msbuild/2003"

	function Select-MsBuildNodes([string] $xpath) {
		if ($hasNs) {
			return $xml.SelectNodes($xpath.Replace("//", "//ms:").Replace("/", "/ms:"), $nsmgr)
		}
		return $xml.SelectNodes($xpath)
	}

	$packages = New-Object System.Collections.Generic.List[string]

	$refs = @(Select-MsBuildNodes("//ProjectReference"))
	foreach ($ref in $refs) {
		$include = $ref.GetAttribute("Include")
		$fileName = Split-Path $include -Leaf
		if ($dropProjectFiles -contains $fileName) {
			$null = $ref.ParentNode.RemoveChild($ref)
			continue
		}
		$packageId = $packageByProjectFile[$fileName]
		if ($null -eq $packageId) {
			# Sibling project inside the template (e.g. shared app from a platform head).
			continue
		}
		if (-not $packages.Contains($packageId)) {
			$packages.Add($packageId)
		}
		$null = $ref.ParentNode.RemoveChild($ref)
	}

	$imports = @(Select-MsBuildNodes("//Import"))
	foreach ($import in $imports) {
		$project = $import.GetAttribute("Project")
		if ($project -match "Presentation\.props") {
			$null = $import.ParentNode.RemoveChild($import)
			if (-not $packages.Contains("Cornerstone.Presentation")) {
				$packages.Add("Cornerstone.Presentation")
			}
		}
	}

	# Drop empty ItemGroups left behind
	$itemGroups = @(Select-MsBuildNodes("//ItemGroup"))
	foreach ($group in $itemGroups) {
		if (-not $group.HasChildNodes) {
			$null = $group.ParentNode.RemoveChild($group)
		}
	}

	if ($packages.Count -gt 0) {
		$itemGroup = $xml.CreateElement("ItemGroup")
		foreach ($packageId in ($packages | Sort-Object)) {
			$pkg = $xml.CreateElement("PackageReference")
			$pkg.SetAttribute("Include", $packageId)
			$pkg.SetAttribute("Version", $packageVersionToken)
			$null = $itemGroup.AppendChild($pkg)
		}
		$null = $xml.DocumentElement.AppendChild($itemGroup)
	}

	$settings = New-Object System.Xml.XmlWriterSettings
	$settings.Indent = $true
	$settings.IndentChars = "`t"
	$settings.OmitXmlDeclaration = $true
	$settings.NewLineChars = "`r`n"
	$settings.Encoding = New-Object System.Text.UTF8Encoding $false
	$writer = [System.Xml.XmlWriter]::Create($ProjectPath, $settings)
	try {
		$xml.Save($writer)
	}
	finally {
		$writer.Dispose()
	}

	$rewritten = Get-Content $ProjectPath -Raw
	if ($rewritten -match "Presentation\.props") {
		throw "Generated project still imports Presentation.props: $ProjectPath"
	}

	$remaining = @(Select-MsBuildNodes("//ProjectReference"))
	foreach ($ref in $remaining) {
		$fileName = Split-Path $ref.GetAttribute("Include") -Leaf
		if ($packageByProjectFile.ContainsKey($fileName) -or ($dropProjectFiles -contains $fileName)) {
			throw "Generated project still references Cornerstone project '$fileName': $ProjectPath"
		}
	}
}

function Convert-TemplateJson {
	param ([string] $JsonPath)

	$json = Get-Content $JsonPath -Raw
	$updated = [regex]::Replace(
		$json,
		'("CornerstoneVersion"\s*:\s*\{[^}]*"defaultValue"\s*:\s*")([^"]*)(")',
		{ param ($m) $m.Groups[1].Value + $cornerstoneVersion + $m.Groups[3].Value }
	)
	Set-Content -Path $JsonPath -Value $updated.TrimEnd() -NoNewline -Encoding utf8
	Add-Content -Path $JsonPath -Value "" -Encoding utf8
}

foreach ($template in $templates) {
	$sourceDir = Join-Path $TemplatesRoot $template.Id
	if (-not (Test-Path $sourceDir)) {
		throw "Template source not found: $sourceDir"
	}

	$destDir = Join-Path $ContentRoot $template.Id
	if (Test-Path $destDir) {
		Remove-Item -Recurse -Force $destDir
	}

	$sourceRoot = (Get-Item $sourceDir).FullName
	Get-ChildItem $sourceDir -Recurse -File | ForEach-Object {
		$relative = $_.FullName.Substring($sourceRoot.Length).TrimStart("\", "/")
		if (Test-ShouldSkipPath $relative) {
			return
		}
		$dest = Join-Path $destDir $relative
		$destParent = Split-Path $dest -Parent
		if (-not (Test-Path $destParent)) {
			New-Item -ItemType Directory -Force -Path $destParent | Out-Null
		}
		Copy-Item $_.FullName $dest
	}

	Get-ChildItem $destDir -Recurse -Filter "*.csproj" | ForEach-Object {
		Convert-ProjectFile -ProjectPath $_.FullName
	}

	Get-ChildItem $destDir -Recurse -Filter "template.json" | ForEach-Object {
		Convert-TemplateJson -JsonPath $_.FullName
	}

	Write-Host "Converted $($template.Id) -> $destDir (CornerstoneVersion default $cornerstoneVersion)"
}
