Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
#Requires –Version 3.0
#------------------------------

function Get-UpdateComPath ($packageId, $baseDir){

	$squirrelDir = Join-Path $baseDir "$($Metadata.Common.EnvironmentName)\$($Metadata.Common.Version.Branch)"

	# squirrel limitation: packageId should be last element in path
	$packageId = $packageId.Replace('.', $null).Replace('-', $null)
	$packageDir = Join-Path $squirrelDir $packageId
	if (!(Test-Path $packageDir)){
		md $packageDir | Out-Null 
	}

	$updateComPath = Join-Path $packageDir 'Update.com'
	if (!(Test-Path $updateComPath)){

		$squirrelTempDir = Join-Path $squirrelDir 'SquirrelTemp'
		if (!(Test-Path $squirrelTempDir)){
			md $squirrelTempDir | Out-Null
		}

		$updateZipFileName = Join-Path $squirrelTempDir 'Update.zip'
		if (!(Test-Path $updateZipFileName)){
			# convention
			$updateZipUrl = "$($Metadata.Squirrel.UpdateServerUrl)/Update.zip"
			Invoke-WebRequest $updateZipUrl -OutFile $updateZipFileName | Out-Null	
		}
					
		$readFileStream = New-Object System.IO.FileStream($updateZipFileName, 'Open')
		Add-Type -AssemblyName System.IO.Compression
		$zipArchive = New-Object System.IO.Compression.ZipArchive($readFileStream)
		foreach($entry in $zipArchive.Entries){
			$entryPath = Join-Path $packageDir $entry.Name
			$writeFileStream = New-Object System.IO.FileStream($entryPath, 'Create')
			$zipStream = $entry.Open()
			$zipStream.CopyTo($writeFileStream)
			$writeFileStream.Dispose()
		}
		$zipArchive.Dispose()

		if (!(Test-Path $updateComPath)){
			throw "Can't find Update.com"
		}
	}

	return $updateComPath
}

function Invoke-UpdateExe ($packageId, $baseDir, $Arguments){

	$updateComPath = Get-UpdateComPath $packageId $baseDir

	& $updateComPath $Arguments 2>&1

	if ($LastExitCode -ne 0){
		throw "Update.com exited with exit code $LastExitCode"
	}
}

function Update-SquirrelPackage ($packageId, $baseDir){
	$updateServerUrlForPackage = '{0}/{1}/{2}/{3}' -f `
		$Metadata.Squirrel.UpdateServerUrl, `
		$Metadata.Common.EnvironmentName, `
		$Metadata.Common.Version.Branch, `
		$packageId

	Invoke-UpdateExe $packageId $baseDir @(
		'--update'
		$updateServerUrlForPackage
	)
}

function Uninstall-SquirrelPackage ($packageId, $baseDir) {
	Invoke-UpdateExe $packageId $baseDir @(
		'--uninstall'
	)

	$envDir = Join-Path $baseDir $Metadata.Common.EnvironmentName

	# remove package folders
	Get-ChildItem $envDir -Filter $packageId -Recurse | `
	Where { $_.PSIsContainer } | `
	Remove-Item -Force
}

Export-ModuleMember -Function Update-SquirrelPackage, Uninstall-SquirrelPackage