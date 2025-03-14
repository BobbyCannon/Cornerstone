param
(
	[Parameter()]
	[Switch] $Preview
)

$productName = "Cornerstone"
$scriptPath = $PSScriptRoot

if ($scriptPath.Length -le 0)
{
	$scriptPath = "C:\Workspaces\GitHub\$productName"
}


#$files = Get-Item "$scriptPath\Binaries\*.nupkg"
$file = [System.IO.FileInfo] "$scriptPath\Directory.Build.props"
$fileXml = [xml](Get-Content $file.FullName -Raw)
$version = $fileXml.Project.PropertyGroup.AssemblyVersion.ToString();

$files = @()
$projects = @("Cornerstone","Cornerstone.Automation","Cornerstone.Avalonia",
	"Cornerstone.EntityFramework","Cornerstone.Newtonsoft","BobsToolbox.PowerShell",
	"Cornerstone.Web"
)

foreach ($project in $projects)
{
	$files += "$scriptPath\$project\bin\Release\$project.$version.nupkg"
}

foreach ($file in $files)
{
	$fileInfo = [System.IO.FileInfo] $file
	
	if ($Preview.IsPresent)
	{
		Write-host "& `"nuget.exe`" push $($fileInfo.FullName) -Source https://api.nuget.org/v3/index.json" -ForegroundColor Yellow
		continue
	}
	
	Write-Host $fileInfo.FullName -ForegroundColor Cyan
	
	& "nuget.exe" push $fileInfo.FullName -Source https://api.nuget.org/v3/index.json
	
	if (Test-Path "C:\Workspaces\Nuget\Release")
	{
		Copy-Item $fileInfo.FullName "C:\Workspaces\Nuget\Release\$($fileInfo.Name)"
	}
}