Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
#Requires –Version 3.0
#------------------------------

Import-Module "$BuildToolsRoot\modules\entrypoint.psm1" -DisableNameChecking
Import-Module "$BuildToolsRoot\modules\nuget.psm1" -DisableNameChecking

Task Build-HostsUpdates -Precondition { $Metadata['PublishUpdatesForHosts'] } {
	$hosts = $Metadata['PublishUpdatesForHosts']
	foreach ($host in $hosts.GetEnumerator()){
		$commonMetadata = $Metadata.Common

		$version = $commonMetadata.Version.NuGetVersion
		$packageId = $host.Replace(".", "")
		$nuspec = @"
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd">
  <metadata>
    <id>$packageId</id>
    <version>$version</version>
    <title>$host</title>
    <authors>2GIS</authors>
    <description>$host</description>
    <dependencies />
  </metadata>
  <files>
    <file src="*.*" target="lib\net461\" />
  </files>
</package>
"@
		$nuspecPath = Join-Path $commonMetadata.Dir.Temp ($host + '.nuspec') 
		Set-Content $nuspecPath $nuspec
		
		$nupkgDir = Join-Path $commonMetadata.Dir.Temp $host
		if(!(Test-Path $nupkgDir)){
			mkdir "$nupkgDir"
		}
		
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

Task Run-PublishUpdates -Precondition { $Metadata['PublishUpdatesForHosts'] } {
	$hosts = $Metadata['PublishUpdatesForHosts']
	foreach ($host in $hosts.GetEnumerator()){
		$artifactName = Get-Artifacts 'NuGet'

		$nupkgPath = Get-ChildItem $artifactName -Filter '*.nupkg'

		$squirrelPackageInfo = Get-PackageInfo 'squirrel.windows'
		$squirrel = Join-Path (Join-Path  $squirrelPackageInfo.VersionedDir 'tools') 'Squirrel.exe'

		#TODO: Specify environment index
		$publishPath = Join-Path '\\uk-erm-test01\c$\inetpub\updates.test.erm.2gis.ru\Test.21' $host
		
		Write-Host 'Invoke Squirrel --releasify for' $nupkgPath ', release directory' $publishPath
		& $squirrel --releasify $nupkgPath.FullName -releaseDir $publishPath --no-msi | Write-Host

		if ($LastExitCode -ne 0) {
			throw "Command failed with exit code $LastExitCode"
		}
	}
}

Task Run-UpdateHosts -Precondition { $Metadata['HostsToUpdate'] } {
	$hosts = $Metadata['HostsToUpdate']

	$psExecPackageInfo = Get-PackageInfo 'psexec.exe'
	$psExec = Join-Path $psExecPackageInfo.VersionedDir 'psexec.exe'

	foreach ($host in $hosts.GetEnumerator()){

		$entryPointMetadata = $Metadata[$host]
		$serviceNames = Get-WinServiceNames $entryPointMetadata
		$packageId = $host.Replace(".", "")

		foreach($targetHost in $entryPointMetadata.TargetHosts){

			$session = Get-CachedSession $targetHost
			$result = Invoke-Command $session {

				$servicePath = "${Env:WinDir}\ServiceProfiles\NetworkService\AppData\Local\$using:packageId"
				$appPath = Get-ChildItem $servicePath | where { $_.PSIsContainer } | select -First 1
				$serviceExePath = Get-ChildItem $appPath.FullName -Filter '*.exe'
				$updateExePath = Join-Path $servicePath 'Update.exe'

				return @{
					'UpdateExePath' = $updateExePath
					'ServiceExeName' = $serviceExePath.Name
				}
			}

			$targetHostPath = "\\$targetHost"

			$uninstallArgs = "uninstall -servicename \`"$($serviceNames.Name)\`""
			Write-Host "Executing $($result.UpdateExePath) on $targetHostPath with $psExec, arguments: $uninstallArgs"
			& $psExec $targetHostPath -accepteula -h $result.UpdateExePath --processStart $result.ServiceExeName --process-start-args $uninstallArgs

			Start-Sleep -Seconds 5

			$installArgs = "install -servicename \`"$($serviceNames.Name)\`" -displayname \`"$($serviceNames.VersionedDisplayName)\`" start"
			Write-Host "Executing $($result.UpdateExePath) on $targetHostPath with $psExec, arguments: $installArgs"
			& $psExec $targetHostPath -accepteula -h $result.UpdateExePath --processStart $result.ServiceExeName --process-start-args $installArgs
		}
	}
}

#WARNING: copypasted from deploy.psm1
function Get-WinServiceNames ($entryPointMetadata) {

	$commonMetadata = $Metadata.Common
	$semanticVersion = $commonMetadata.Version.SemanticVersion

	$serviceName = $entryPointMetadata.ServiceName
	$serviceDisplayName = $entryPointMetadata.ServiceDisplayName


	if ($commonMetadata['EnvironmentName']){
		$environmentName = $commonMetadata.EnvironmentName

		$name = "$serviceName-$environmentName"
		$displayName = "$serviceDisplayName ($environmentName)"
	} else {
		$name = $serviceName
		$displayName = $serviceDisplayName
	}

	$versionedName = "$name-$semanticVersion"
	$versionedDisplayName = "$displayName ($semanticVersion)"

	return @{
		'Name' = $name
		'VersionedName' = $versionedName

		'DisplayName' = $displayName
		'VersionedDisplayName' = $versionedDisplayName
	}
}