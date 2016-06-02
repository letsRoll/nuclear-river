Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
#Requires –Version 3.0
#------------------------------

Import-Module "$BuildToolsRoot\modules\entrypoint.psm1" -DisableNameChecking
Import-Module "$BuildToolsRoot\modules\nuget.psm1" -DisableNameChecking
Import-Module "$BuildToolsRoot\modules\winrm.psm1" -DisableNameChecking

Task Run-InstallHosts -Precondition { $Metadata['HostsToInstall'] } {
	$hostsToInstall = $Metadata['HostsToInstall']
	foreach ($host in $hostsToInstall.GetEnumerator()){
		$entryPointMetadata = $Metadata[$host]
		foreach($targetHost in $entryPointMetadata.TargetHosts){
			$session = Get-CachedSession $targetHost
			Invoke-Command $session {
				$setupDir = Join-Path (Join-Path $using:commonMetadata.Dir.Temp 'Setup') $using:host
				$unused = New-Item $setupDir -ItemType Directory

				$webClient = New-Object System.Net.WebClient
				$webClient.UseDefaultCredentials = $true
				$webClient.Proxy.Credentials = $webClient.Credentials
				#TODO: Specify environment index
				$webClient.DownloadFile('http://updates.test.erm.2gis.ru/Test.21/CustomerIntelligence.Replication.Host/Setup.exe', $setupDir)
				
				$credential = New-Object System.Management.Automation.PSCredential('NT AUTHORITY\NETWORK SERVICE', $emptyPassword)
				Start-Process -FilePath (Join-Path $setupDir 'Setup.exe') -Credential $credential
			}
		}
	}
}