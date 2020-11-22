# Script for installing the TransMock test utils assembly for BizTalk server to the GAC
param($installPath, $toolsPath, $package, $project)

$DebugPreference = "Continue"

Write-Debug "installPath parameter is: $installPath"
Write-Debug "toolsPath parameter is: $toolsPath"
Write-Debug "package parameter is: $package"
Write-Debug "project parameter is: $project"


function ConfigureTools{
	param([string]$BTSVersion)

	Write-Host "ConfigureTools called with BTSVersion: $BTSVersion"

	# here we install the adapter for the correct .NET version
	$TargetDirectory = Get-ChildItem $toolsPath -Filter "*$BTSVersion" -Recurse -Directory
	if($TargetDirectory)
	{
		Write-Host "TargetDirectory is: $($TargetDirectory.FullName)"
	
		# Replace the macros for BizTalk and package version accordingly
		# in the TransMock.targets file 
		# First we open the TransMock.Targets file
		$TargetsFilePath = "$installPath\BTDF\TransMock.targets"

		Write-Debug "TargetsFilePath is: $TargetsFilePath"
		$TargetsFile = (Get-Content $TargetsFilePath)		

		# Replace the {PkgVersion} macro with the current package version
		$TargetsFile = $TargetsFile -replace "{PkgVersion}","$($package.Version)"
			
		# Replace the {BizTalkVersion} macro with the current package version
		$TargetsFile = $TargetsFile -replace "{BizTalkVersion}", "BTS$BTSVersion"
		
		Set-Content $TargetsFilePath $TargetsFile
	}
	else{
		Write-Error "$TargetDirectory folder not found"
	}
}

# 1. First identify which version of BizTalk server is installed
$bizTalkRegistryPath = "HKLM:\SOFTWARE\Microsoft\BizTalk Server\3.0"

if ((Test-Path $bizTalkRegistryPath) -eq $true){
	# Set location to BizTalk registry key    
    $key = Get-Item $bizTalkRegistryPath
    # Get version number. 
    [string]$productVersion = $key.GetValue("ProductVersion")
	
	Write-Host "Product version is: $productVersion"

	# 2. Install the correct adapter as binding
	switch($productVersion){
		# BizTalk 2010
		{ $productVersion -match "3.9.*" }  { ConfigureTools "2010" }
		# BizTalk 2013
		{ $productVersion -match "3.10.*"}  { ConfigureTools "2013" }
		# BizTalk 2013R2
		{ $productVersion -match "3.11.*" } { ConfigureTools "2013R2" }
		# BizTalk 2016
		{ $productVersion -match "3.12.*" } { ConfigureTools "2016" }
		# BizTalk 2020
		{ $productVersion -match "3.13.*" } { ConfigureTools "2020" }
		# deafult
		default { Write-Error "No suitable adapter version found for the currently installed BizTalk Server version";break }
	}
}	
else
{
	Write-Error "Unable to find installed BizTalk Server instance!"		
}