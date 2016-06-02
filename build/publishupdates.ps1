Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
#Requires –Version 3.0
#------------------------------

Import-Module "$BuildToolsRoot\modules\entrypoint.psm1" -DisableNameChecking
Import-Module "$BuildToolsRoot\modules\nuget.psm1" -DisableNameChecking

Task Build-HostsUpdates -Precondition { $Metadata['HostsToUpdate'] } {
	$hostsToUpdate = $Metadata['HostsToUpdate']
	foreach ($host in $hostsToUpdate.GetEnumerator()){
		$commonMetadata = $Metadata.Common

		$version = $commonMetadata.Version.NuGetVersion
		$nuspec = @"
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd">
  <metadata>
    <id>$host</id>
    <version>$version</version>
    <title>$host</title>
    <authors>2GIS</authors>
    <description>$host</description>
    <dependencies />
  </metadata>
  <files>
    <file src="*.*" target="lib\net461\" exclude="*.pdb;*.nupkg;*.vshost.*"/>
  </files>
</package>
"@
		$nuspecPath = Join-Path $commonMetadata.Dir.Temp ($host + '.nuspec') 
		Set-Content $nuspecPath $nuspec
		
		$nupkgDir = Join-Path $commonMetadata.Dir.Temp $host
		$unused = New-Item $nupkgDir -ItemType Directory
		
		Invoke-NuGet @(
			'pack'
			$nuspecPath
			'-OutputDirectory'
			$nupkgDir
			'-BasePath'
		    Get-Artifacts $host
		)
		
		Publish-Artifacts $nupkgDir 'NuGet'
	}
}

Task Run-PublishUpdates -Precondition { $Metadata['HostsToUpdate'] } {
	$hostsToUpdate = $Metadata['HostsToUpdate']
	foreach ($host in $hostsToUpdate.GetEnumerator()){
		$artifactName = Get-Artifacts 'NuGet'

		$nupkgPath = Get-ChildItem $artifactName -Filter '*.nupkg'

		$squirrelPackageInfo = Get-PackageInfo 'squirrel.windows'
		$squirrel = Join-Path (Join-Path  $squirrelPackageInfo.VersionedDir 'tools') 'Squirrel.exe'

		#TODO: Specify environment index
		$publishPath = '\\uk-erm-test01\c$\inetpub\updates.test.erm.2gis.ru\Test.21'
		
		Write-Host 'Invoke Squirrel --releasify for' $nupkgPath ', release directory' $publishPath
		& $squirrel --releasify $nupkgPath.FullName -releaseDir $publishPath --no-msi | Write-Host

		if ($LastExitCode -ne 0) {
			throw "Command failed with exit code $LastExitCode"
		}
	}
}