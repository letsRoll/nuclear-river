Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
#Requires –Version 3.0
#------------------------------

Import-Module "$BuildToolsRoot\modules\entrypoint.psm1" -DisableNameChecking
Import-Module "$BuildToolsRoot\modules\metadata.psm1" -DisableNameChecking
Import-Module "$PSScriptRoot\squirrel.local.psm1" -DisableNameChecking

Task Publish-SquirrelPackages {

	$packagesVersion = $Metadata.Common.Version.NuGetVersion

	$entryPointNames = $Metadata['SquirrelEntryPoints']
	foreach ($entryPointName in $entryPointNames){

		if($Metadata[$entryPointName]){
			$artifacts = Get-Artifacts $entryPointName
			$packageFileName = Create-SquirrelPackage $entryPointName $packagesVersion $artifacts

			Publish-SquirrelPackage $packageFileName $entryPointName
		}
	}
}

function QueueDeploy-SquirrelPackages {

	$entryPointNames = $Metadata['SquirrelEntryPoints']
	foreach ($entryPointName in $entryPointNames){

		if($Metadata[$entryPointName]){
			Add-DeployQueue $entryPointName {
				param($localBuildToolsRoot, $localPSScriptRoot, $localMetadata, $localEntryPointName)

				Import-Module "$localBuildToolsRoot\modules\winrm.psm1" -DisableNameChecking
			
				$metadataModulePath = "$localBuildToolsRoot\modules\metadata.psm1"
				$squirrelRemoteModulePath = "$localPSScriptRoot\squirrel.remote.psm1"

				$entryPointMetadata = $localMetadata[$localEntryPointName]
				foreach($targetHost in $entryPointMetadata.TargetHosts){
			
					$session = Get-CachedSession $targetHost
					Import-ModuleToSession $session $metadataModulePath
					Import-ModuleToSession $session $squirrelRemoteModulePath
	
					Invoke-Command $session {
						param($metadata, $packageId)

						Add-Metadata $metadata

						$appBaseDir = "${Env:SystemRoot}\ServiceProfiles\NetworkService\AppData\Local"

						Update-SquirrelPackage $packageId $appBaseDir

					} -ArgumentList @($localMetadata, $localEntryPointName)
				}

			} -ArgumentList @($BuildToolsRoot, $PSScriptRoot, $Metadata, $entryPointName)
		}
	}	
}