# Script for configuring the TransMock.targets file for working correctly undr BTDF
param($installPath, $toolsPath, $package, $project)

$DebugPreference = "Continue"

Write-Debug "installPath parameter is: $installPath"
Write-Debug "toolsPath parameter is: $toolsPath"
Write-Debug "package parameter is: $package"
Write-Debug "project parameter is: $project"


function ConfigureTools{
	param([string]$BTSVersion)
	
	# Replace the macros package version
	# in the TransMock.targets file 
	# First we open the TransMock.Targets file
	$TargetsFilePath = "$installPath\BTDF\TransMock.targets"

	Write-Host "TargetsFilePath is: $TargetsFilePath"

	if(Test-Path $TargetsFilePath)
	{
		Write-Debug "TargetsFilePath is: $TargetsFilePath"
		$TargetsFile = (Get-Content $TargetsFilePath)		

		# Replace the {PkgVersion} macro with the current package version
		$TargetsFile = $TargetsFile -replace "{PkgVersion}","$($package.Version)"
			
		# Replace the {BizTalkVersion} macro with the current package version
		#$TargetsFile = $TargetsFile -replace "{BizTalkVersion}", "BTS$BTSVersion"
		
		Set-Content $TargetsFilePath $TargetsFile
	}
	else{
		Write-Error "$TargetsFilePath file not found"
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

	# 2. Configure tools
	ConfigureTools ""
}	
else
{
	Write-Error "Unable to find installed BizTalk Server instance!"		
}