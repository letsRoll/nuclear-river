Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
#Requires –Version 3.0
#------------------------------

Import-Module "$BuildToolsRoot\modules\nuget.psm1" -DisableNameChecking
Import-Module "$BuildToolsRoot\modules\metadata.psm1" -DisableNameChecking

$packageInfo = Get-PackageInfo 'squirrel.windows'
$squirrelPath = Join-Path (Join-Path  $packageInfo.VersionedDir 'tools') 'Squirrel.com'

function Create-SquirrelPackage ($packageId, $version, $dir){
	# squirrel limitation
	$packageId = $packageId.Replace('.', $null).Replace('-', $null)

	$nuSpecFileName = Get-SquirrelNuSpecFileName $packageId $version $dir

	$hostTempDir = Join-Path $Metadata.Common.Dir.Temp "Squirrel\$host"
	md $hostTempDir -Force | Out-Null

	Invoke-NuGet @(
		'pack'
		$nuSpecFileName
		'-OutputDirectory'
		$hostTempDir
		'-NoDefaultExcludes'
		'-NoPackageAnalysis'
	)

	$package = @(Get-ChildItem $hostTempDir -Filter '*.nupkg')
	if ($package.Count -ne 1){
		throw "Only one nuget package per host supported"
	}

	return $package.FullName
}

function Get-SquirrelNuSpecFileName ($packageId, $version, $dir) {

		$newContent = @"
<?xml version="1.0"?>
<package>
  <metadata>
  	<!-- NuGet required fields -->
	<id>$packageId</id>
	<version>$version</version>
	<authors>2GIS</authors>
	<description>$packageId</description>
	<!-- Squirrel required fields -->
	<releaseNotes>release notes</releaseNotes>
  </metadata>
  <files>
	<!-- squirrel requirement: place files to lib\net45 -->
    <file src="$dir\**\*.*" target="lib\net45" />
  </files>
</package>
"@
	$tempDir = Join-Path $Metadata.Common.Dir.TempPersist 'Squirrel'
	if (!(Test-Path $tempDir)){
		md $tempDir -Force | Out-Null
	}

	$nuspecFileName = Join-Path $tempDir "$packageId.nuspec"
	Set-Content $nuspecFileName $newContent -Encoding UTF8

	return $nuspecFileName
}

function Publish-SquirrelPackage ($packageFileName, $entryPointMetadataKey){

	$releaseDir = '{0}\{1}\{2}\{3}' -f `
		$Metadata.Squirrel.PublishSource, `
		$Metadata.Common.EnvironmentName, `
		$Metadata.Common.Version.Branch, `
		$entryPointMetadataKey

	& $squirrelPath @(
		'--releasify'
		$packageFileName
		'-releaseDir'
		$releaseDir
		'--no-msi'
	) 2>&1

	if ($LastExitCode -ne 0) {
		throw "Command failed with exit code $LastExitCode"
	}
}

Export-ModuleMember -Function Create-SquirrelPackage, Publish-SquirrelPackage