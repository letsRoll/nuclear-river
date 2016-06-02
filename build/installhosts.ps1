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
			$session = Get-CachedSession $targetHost
			Invoke-Command $session {
				Set-StrictMode -Version Latest
				$ErrorActionPreference = 'Stop'
				#Requires –Version 3.0
				#------------------------------
				
				$setupDir = Join-Path $env:TEMP $using:host
				$unused = New-Item $setupDir -ItemType Directory

				$setupExe = (Join-Path $setupDir 'Setup.exe')
				#TODO: Specify environment index
				Invoke-WebRequest 'http://updates.test.erm.2gis.ru/Test.21/CustomerIntelligence.Replication.Host/Setup.exe' -OutFile $setupExe
				
				$emptyPassword = New-Object System.Security.SecureString
				$credential = New-Object System.Management.Automation.PSCredential('NT AUTHORITY\NETWORK SERVICE', $emptyPassword)
				Start-Process -FilePath (Join-Path $setupDir 'Setup.exe') -Credential $credential
			}
		}
	}
}