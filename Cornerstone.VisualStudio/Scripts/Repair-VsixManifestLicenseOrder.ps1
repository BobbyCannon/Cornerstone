param(
	[Parameter(Mandatory = $true)]
	[string] $Path
)

# DetokenizeVsixManifestSource writes License after PreviewImage.
# PackageManifestSchema.Metadata.xsd requires License before Icon.
$xml = New-Object System.Xml.XmlDocument
$xml.PreserveWhitespace = $true
$xml.Load($Path)
$namespace = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
$namespace.AddNamespace('m', 'http://schemas.microsoft.com/developer/vsx-schema/2011')
$metadata = $xml.SelectSingleNode('//m:Metadata', $namespace)
if ($null -eq $metadata) {
	return
}

$license = $metadata.SelectSingleNode('m:License', $namespace)
$icon = $metadata.SelectSingleNode('m:Icon', $namespace)
if (($null -eq $license) -or ($null -eq $icon)) {
	return
}

[void]$metadata.InsertBefore($license, $icon)
$xml.Save($Path)
