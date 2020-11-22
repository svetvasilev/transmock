# Script for installing the TransMock test utils assembly for BizTalk server to the GAC
param($installPath, $toolsPath, $package, $project)

$DebugPreference = "Continue"

Write-Debug "installPath parameter is: $installPath"
Write-Debug "toolsPath parameter is: $toolsPath"
Write-Debug "package parameter is: $package"
Write-Debug "project parameter is: $project"

# Cosntants
$BIZTALK_TESTUTILS_ASSEMBLY_NAME = "TransMock.TestUtils.BizTalk.dll"

function InstallTestUtils{
	param([string]$BTSVersion)

	Write-Host "InstallAdapter called with BTSVersion: $BTSVersion"

	[string]$TargetNetVersion = ""
	[string]$WinSDKFilter = ""

	switch($BTSVersion){
		"2010" { 
			$TargetNetVersion = "net40"
			$WinSDKFilter="NETFX 4.0 Tools"
		}
		"2013" { 
			$TargetNetVersion = "net45"
			$WinSDKFilter="NETFX 4.0 Tools"
		}
		"2013R2" { 
			$TargetNetVersion = "net451"
			$WinSDKFilter="NETFX 4.5.1 Tools"
		}
		"2016" { 
			$TargetNetVersion = "net46"
			$WinSDKFilter="NETFX 4.6 Tools"
		}
		"2020" { 
			$TargetNetVersion = "net48"
			$WinSDKFilter="NETFX 4.8 Tools"
		}
	}

	Write-Debug "TargetNetVersion is:$TargetNetVersion,  WinSDKFilter is: $WinSDKFilter"

	# here we install the adapter for the correct .NET version
	$TargetDirectory = Get-ChildItem $installPath -Filter "*$TargetNetVersion" -Recurse -Directory
	if($TargetDirectory)
	{
		Write-Host "TargetDirectory is: $($TargetDirectory.FullName)"
	
		# GAC the test utils assembly
		# First we need to find gacutil
		$WinSDKDirectory = "C:\Program Files\Microsoft SDKs\Windows"		

		if([System.Environment]::Is64BitOperatingSystem -eq $true){
			$WinSDKDirectory = $WinSDKDirectory -replace "Program Files","Program Files (x86)"
		}
			
		$GacUtilDir = Get-ChildItem $WinSDKDirectory -Filter "*$WinSDKFilter*" -Recurse -Directory | `
			Select-Object -Last 1
			
		if($GacUtilDir){
			Write-Debug "GacUtilDir found and is: $($GacUtilDir.FullName)"

			Write-Output "Invoking gacutil.exe to GAC the test utils assembly"
			$GacUtilOutput = & "$($GacUtilDir.FullName)\gacutil.exe" "/i" "$($TargetDirectory.FullName)\$BIZTALK_TESTUTILS_ASSEMBLY_NAME"
			Write-Output "gacutil.exe returned: $GacUtilOutput"
		}
		else{
			Write-Error "Did not find the correct gacutil.exe version!"
		}		
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
		{ $productVersion -match "3.9.*" }  { InstallTestUtils "2010" }
		# BizTalk 2013
		{ $productVersion -match "3.10.*"}  { InstallTestUtils "2013" }
		# BizTalk 2013R2
		{ $productVersion -match "3.11.*" } { InstallTestUtils "2013R2" }
		# BizTalk 2016
		{ $productVersion -match "3.12.*" } { InstallTestUtils "2016" }
		# BizTalk 2020
		{ $productVersion -match "3.13.*" } { InstallTestUtils "2020" }
		# deafult
		default { Write-Error "No suitable adapter version found for the currently installed BizTalk Server version";break }
	}
}	
else
{
	Write-Error "Unable to find installed BizTalk Server instance!"		
}