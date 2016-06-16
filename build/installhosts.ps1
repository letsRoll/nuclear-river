Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
#Requires –Version 3.0
#------------------------------

Import-Module "$BuildToolsRoot\modules\deploy.psm1" -DisableNameChecking
Import-Module "$BuildToolsRoot\modules\entrypoint.psm1" -DisableNameChecking
Import-Module "$BuildToolsRoot\modules\nuget.psm1" -DisableNameChecking
Import-Module "$BuildToolsRoot\modules\winrm.psm1" -DisableNameChecking

Task Run-InstallHosts -Precondition { $Metadata['HostsToInstall'] } {
	$hosts = $Metadata['HostsToInstall']
	$commonMetadata = $Metadata.Common
	
	$psExecPackageInfo = Get-PackageInfo 'psexec.exe'
	$psExec = Join-Path $psExecPackageInfo.VersionedDir 'psexec.exe'

	foreach ($host in $hosts.GetEnumerator()){

		$entryPointMetadata = $Metadata[$host]
		$serviceNames = Get-WinServiceNames $entryPointMetadata
		$packageId = $host.Replace(".", "")
		
		foreach($targetHost in $entryPointMetadata.TargetHosts){
			
			$setupDir = Join-Path $commonMetadata.Dir.Temp $host
 			if(!(Test-Path $setupDir)){
				mkdir "$setupDir"
			}

			$setupExe = Join-Path $setupDir 'Setup.exe'
			#TODO: Specify environment index
			$setupUrl = "http://updates.test.erm.2gis.ru/Test.21/$host/Setup.exe"
			
			Write-Host "Dowloading setup.exe from $setupUrl"
			(New-Object System.Net.WebClient).DownloadFile($setupUrl, $setupExe)
			
			$stopUrl = "http://$($targetHost):5000/$($serviceNames.Name)/stop"
			Write-Host "Stopping service if it has been installed and is running, URL: $stopUrl"			
			Invoke-WebRequest -Uri $stopUrl -Method POST

			Start-Sleep -Seconds 5

			$targetHostPath = "\\$targetHost"

			Write-Host "Executing $setupExe on $targetHostPath with $psExec"
			& $psExec $targetHostPath -accepteula -u 'NT AUTHORITY\NETWORK SERVICE' -cf $setupExe

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