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
	foreach ($host in $hosts.GetEnumerator()){

		$entryPointMetadata = $Metadata[$host]
		$serviceNames = Get-WinServiceNames $entryPointMetadata
		$packageId = $host.Replace(".", "")
		
		foreach($targetHost in $entryPointMetadata.TargetHosts){
			
			$setupDir = Join-Path $commonMetadata.Dir.Temp $host
 			if(!(Test-Path $setupDir)){
				mkdir "$setupDir"
			}

			$setupExe = (Join-Path $setupDir 'Setup.exe')
			#TODO: Specify environment index
			$setupUrl = 'http://updates.test.erm.2gis.ru/Test.21/' + $host + '/Setup.exe'
			
			Write-Host 'Dowloading setup.exe from' $setupUrl
			(New-Object System.Net.WebClient).DownloadFile($setupUrl, $setupExe)
			
			$psExecPackageInfo = Get-PackageInfo 'psexec.exe'
			$psExec = Join-Path $psExecPackageInfo.VersionedDir 'psexec.exe'
			
			Write-Host 'Executing' $setupExe 'remotely with PsExec on path' $psExec
			& $psExec ('\\' + $targetHost) -accepteula -u 'NT AUTHORITY\NETWORK SERVICE' -cf $setupExe

			$session = Get-CachedSession $targetHost
			 Invoke-Command $session {
				
				Write-Host '1'

				$servicePath = "${Env:WinDir}\ServiceProfiles\NetworkService\AppData\Local\$using:packageId"
				$appPath = Get-ChildItem $servicePath | where { $_.PSIsContainer } | select -First 1
				$serviceExePath = Get-ChildItem $appPath.FullName -Filter '*.exe'
				$updateExePath = Join-Path $servicePath 'Update.exe'

				$processStartArg = @(
					'--processStart'
					$serviceExePath.Name
				)

				$uninstallArgs = $processStartArg + @(
					'--process-start-args'
					'"uninstall -servicename \"' + $using:serviceNames.Name + '\""'
				)

				Write-Host '2'
				Write-Host $uninstallArgs

			    & $updateExePath  $uninstallArgs | Out-Host

				Write-Host '3'

				$installArgs = $processStartArg + @(
					'--process-start-args'
					'install -servicename \"' + $using:serviceNames.Name + '\" -displayname \"' + $using:serviceNames.VersionedDisplayName + '\" start'
				)

				#& $updateExePath $installArgs

				Write-Host '4'
			}
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