Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
#Requires –Version 3.0
#------------------------------

Import-Module "$BuildToolsRoot\modules\deploy.psm1" -DisableNameChecking
Import-Module "$BuildToolsRoot\modules\entrypoint.psm1" -DisableNameChecking
Import-Module "$BuildToolsRoot\modules\nuget.psm1" -DisableNameChecking
Import-Module "$BuildToolsRoot\modules\winservice.psm1" -DisableNameChecking
Import-Module "$BuildToolsRoot\modules\winrm.psm1" -DisableNameChecking

#WARNING: this is copypaste from deploy.psm1
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

Task Run-InstallHosts -Precondition { $Metadata['HostsToInstall'] } {
	$hostsToInstall = $Metadata['HostsToInstall']
	$commonMetadata = $Metadata.Common
	foreach ($host in $hostsToInstall.GetEnumerator()){
		$entryPointMetadata = $Metadata[$host]
		$serviceNames = Get-WinServiceNames $entryPointMetadata

		foreach($targetHost in $entryPointMetadata.TargetHosts){
			
			$setupDir = Join-Path $commonMetadata.Dir.Temp $host
 			if(!(Test-Path $setupDir)){
				mkdir "$setupDir"
			}

			$setupExe = (Join-Path $setupDir 'Setup.exe')
			#TODO: Specify environment index
			$setupUrl = 'http://updates.test.erm.2gis.ru/Test.21/' + $host + '/Setup.exe'
			Invoke-WebRequest $setupUrl -OutFile $setupExe | Write-Host
			
			$psExecPackageInfo = Get-PackageInfo 'psexec.exe'
			$psExec = Join-Path $psExecPackageInfo.VersionedDir 'psexec.exe'
			
			& $psExec ('\\' + $targetHost) -accepteula -u 'NT AUTHORITY\NETWORK SERVICE' -e -cf $setupExe | Write-Host
			
			$session = Get-CachedSession $targetHost
			Invoke-Command $session {
				
				Load-WinServiceModule $($using:host)
				Take-WinServiceOffline $($using:host)
			
				Delete-WindowsService $using:serviceNames.Name
				$packageId = $($using:host).Replace(".", "")
				$servicePath = Get-ChildItem "${Env:WinDir}\ServiceProfiles\NetworkService\AppData\Local\$packageId" -Filter '*.exe'
				
				Create-WindowsService $using:serviceNames.VersionedName $using:serviceNames.VersionedDisplayName $servicePath.FullName
				Start-WindowsService $using:serviceNames.VersionedName
			}
		}
	}
}