param([string[]]$TaskList = @(), [hashtable]$Properties = @{})

if ($TaskList.Count -eq 0){
	$TaskList = @('Run-UnitTests', 'Build-NuGet')
}

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
#------------------------------
cls

$Properties.SolutionDir = Join-Path $PSScriptRoot '..'

# Restore-Packages
& {
	$NugetPath = Join-Path $Properties.SolutionDir '.nuget\NuGet_v3.3.0.exe'
	if (!(Test-Path $NugetPath)){
		$webClient = New-Object System.Net.WebClient
		$webClient.UseDefaultCredentials = $true
		$webClient.Proxy.Credentials = $webClient.Credentials
		$webClient.DownloadFile('https://dist.nuget.org/win-x86-commandline/v3.3.0/nuget.exe', $NugetPath)
	}
	$solution = Get-ChildItem $Properties.SolutionDir -Filter '*.sln'
	& $NugetPath @('restore', $solution.FullName, '-NonInteractive', '-Verbosity', 'quiet')
}

$packageName = "2GIS.NuClear.BuildTools"
$packageVersion = (ConvertFrom-Json (Get-Content "$PSScriptRoot\project.json" -Raw)).dependencies.PSObject.Properties[$packageName].Value
Import-Module "${env:UserProfile}\.nuget\packages\$packageName\$packageVersion\tools\buildtools.psm1" -DisableNameChecking -Force
Add-Metadata @{
	'NuGet' = @{
		'Publish' = @{
			'Source' = 'https://www.nuget.org/api/v2/package'
			'PrereleaseSource' = 'http://nuget.2gis.local/api/v2/package'

			'SymbolSource'= 'https://nuget.smbsrc.net/api/v2/package'
			'PrereleaseSymbolSource' = 'http://nuget.2gis.local/SymbolServer/NuGet'
		}
	}
}

Run-Build $TaskList $Properties