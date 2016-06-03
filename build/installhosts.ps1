Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
#Requires –Version 3.0
#------------------------------

Import-Module "$BuildToolsRoot\modules\entrypoint.psm1" -DisableNameChecking
Import-Module "$BuildToolsRoot\modules\nuget.psm1" -DisableNameChecking
Import-Module "$BuildToolsRoot\modules\winrm.psm1" -DisableNameChecking

Task Run-InstallHosts -Precondition { $Metadata['HostsToInstall'] } {
	$hostsToInstall = $Metadata['HostsToInstall']
	$commonMetadata = $Metadata.Common
	foreach ($host in $hostsToInstall.GetEnumerator()){
		$entryPointMetadata = $Metadata[$host]
		foreach($targetHost in $entryPointMetadata.TargetHosts){
			
			$setupDir = Join-Path $commonMetadata.Dir.Temp $host
			if(!(Test-Path $setupDir)){
				mkdir "$setupDir"
			}

			$setupExe = (Join-Path $setupDir 'Setup.exe')
			#TODO: Specify environment index
			Invoke-WebRequest 'http://updates.test.erm.2gis.ru/Test.21/CustomerIntelligence.Replication.Host/Setup.exe' -OutFile $setupExe| Write-Host
			
			$psExecPackageInfo = Get-PackageInfo 'psexec.exe'
			$psExec = Join-Path $psExecPackageInfo.VersionedDir 'psexec.exe'
			
			& $psExec -accepteula ('\\' + $targetHost) -u 'NT AUTHORITY\NETWORK SERVICE' -f -c $setupExe | Write-Host
		}
	}
}