# Script for installing the TransMock Adapter binding as BizTalk adapter
param($installPath, $toolsPath, $package, $project)

Add-Type -AssemblyName "System.Configuration"
Add-Type -AssemblyName "System.ServiceModel"
# Add-Type -AssemblyName "System.Reflection"
$DebugPreference = "Continue"

Write-Debug "installPath parameter is: $installPath"
Write-Debug "toolsPath parameter is: $toolsPath"
Write-Debug "package parameter is: $package"
Write-Debug "project parameter is: $project"

# Cosntants
$BINDING_ASSEMBLY_NAME = "TransMock.Wcf.Adapter"
$BINDINGELEM_NAME = "mockTransport"
$BINDINGELEM_TYPE = "TransMock.Wcf.Adapter.MockAdapterBindingElementExtensionElement"
$BINDING_NAME = "mockBinding"
$BINDING_TYPE = "TransMock.Wcf.Adapter.MockAdapterBindingCollectionElement"
$BINDING_SCHEME = "mock"

function RemoveMachineConfigurationInfo{
	param([System.Configuration.Configuration] $Config)

	Write-Debug "AddMachineConfigurationInfo called with: $Config"
        
    # add <client><endpoint>             
    $SectionGroup = $Config.GetSectionGroup("system.serviceModel") -as [System.ServiceModel.Configuration.ServiceModelSectionGroup]
            
	if ($SectionGroup)
    {
		Write-Host "serviceModel section group found. Continuing processing" -ForegroundColor Cyan
        
		Write-Debug "SectionGroup is: $SectionGroup"
		Write-Debug "Client is: $($SectionGroup.Client)"
		Write-Debug "Client.Endpoints is: $($SectionGroup.Client.Endpoints)"

		# the client endpoint for the mock adapter
		$ChannelEndpointElement = $null
        		
		if($SectionGroup.Client.Endpoints)
		{
			$SectionGroup.Client.Endpoints | % {
				if ($_.Binding.Equals($BINDING_NAME, [System.StringComparison]::OrdinalIgnoreCase) -and
					$_.Name.Equals($BINDING_SCHEME, [System.StringComparison]::OrdinalIgnoreCase) -and
					$_.Contract.Equals("IMetadataExchange", [System.StringComparison]::OrdinalIgnoreCase))
				{
					$ChannelEndpointElement = $_
					
					Write-Debug "The client endpoint found and is: $ChannelEndpointElement"
				}
			}
		}
		
		# removing the client emdpoint first
		if ($ChannelEndpointElement)
		{
			Write-Output "Removing ChannelEndpointElement for : $BINDING_NAME" -Foreground Blue			
            
			$SectionGroup.Client.Endpoints.Remove($ChannelEndpointElement)

			Write-Output "Removed ChannelEndpointElement for : $BINDING_NAME" -Foreground DarkGreen
		}
		
        # remove <bindingExtension>
        if ($SectionGroup.Extensions.BindingExtensions.ContainsKey($BINDING_NAME))
        {
			Write-Output "Removing bindingExtension for : $BINDING_NAME" -Foreground Blue           
            
			$SectionGroup.Extensions.BindingExtensions.RemoveAt($BINDING_NAME)

			Write-Output "Removed bindingExtension for : $BINDING_NAME" -Foreground DarkGreen
        }

		# remove <bindingElementExtension>
        if ($SectionGroup.Extensions.BindingElementExtensions.ContainsKey($BINDINGELEM_NAME))
        {
			Write-Output "Adding bindingElementExtension for : $BINDINGELEM_NAME" -Foreground Blue
			
			$SectionGroup.Extensions.BindingElementExtensions.RemoveAt($BINDINGELEM_NAME)

			Write-Output "Added bindingElementExtension for : $BINDINGELEM_NAME" -Foreground DarkGreen
        }				

        $Config.Save()

		Write-Output "Successfully removed the TransMock adapter for BizTalk" -Foreground DarkGreen
    }
    else {
        Write-Error "Machine.Config doesn't contain system.serviceModel node"
	}
}

function UninstallAdapter{
	param([string]$BTSVersion)

	Write-Host "UninstallAdapter called with BTSVersion: $BTSVersion"

	#[string]$TargetNetVersion = ""
	[string]$WinSDKFilter = ""

	switch($BTSVersion){
		"2010" { 
			#$TargetNetVersion = "net40"
			$WinSDKFilter="NETFX 4.0 Tools"
		}
		"2013" { 
			#$TargetNetVersion = "net45"
			$WinSDKFilter="NETFX 4.0 Tools"
		}
		"2013R2" { 
			#$TargetNetVersion = "net451"
			$WinSDKFilter="NETFX 4.5.1 Tools"
		}
		"2016" { 
			#$TargetNetVersion = "net46"
			$WinSDKFilter="NETFX 4.6 Tools"
		}
	}

	Write-Debug "WinSDKFilter is: $WinSDKFilter"

	[System.Configuration.Configuration]$ConfigFile = [System.Configuration.ConfigurationManager]::OpenMachineConfiguration()
			
	RemoveMachineConfigurationInfo $ConfigFile
			
	# Perform the same operation for the 64-bit framework versions
	if([System.Environment]::Is64BitOperatingSystem -eq $true)
	{
		Write-Output "Removing the binding from the 64-bit machine.config file."
				
		$MachineConfigPathFor64Bit = [System.Runtime.InteropServices.RuntimeEnvironment]::
                GetRuntimeDirectory().Replace("Framework", "Framework64");

        [System.Configuration.ConfigurationFileMap]$ConfigMap = New-Object "System.Configuration.ConfigurationFileMap" `
            -ArgumentList "$($MachineConfigPathFor64Bit)Config\machine.config"

        $ConfigFile = [System.Configuration.ConfigurationManager]::OpenMappedMachineConfiguration($ConfigMap)
                
		Write-Debug "Machine.Config for 64-bit returned is loaded: $ConfigFile"

		RemoveMachineConfigurationInfo $ConfigFile
	}
			
	# UnGAC the adapter
	# First we need to find gacutil
	$WinSDKDirectory = "C:\Program Files\Microsoft SDKs\Windows"
	$DependencyAssemblies = @("TransMock.Wcf.Adapter.Utils","TransMock.Communication.NamedPipes")

	if([System.Environment]::Is64BitOperatingSystem -eq $true){
		$WinSDKDirectory = $WinSDKDirectory -replace "Program Files","Program Files (x86)"
	}
			
	$GacUtilDir = Get-ChildItem $WinSDKDirectory -Filter "*$WinSDKFilter*" -Recurse -Directory | `
		Select-Object -Last 1
			
	if($GacUtilDir){
		Write-Debug "GacUtilDir found and is: $($GacUtilDir.FullName)"

		Write-Output "Invoking gacutil.exe to unGAC the dependency assemblies"
		
		$DependencyAssemblies | % {
				Write-Output "unGACing assembly $_"
				$GacUtilOutput = & "$($GacUtilDir.FullName)\gacutil.exe" "/u" "$_"
				Write-Output "gacutil.exe returned: $GacUtilOutput"
			}

		Write-Output "Invoking gacutil.exe to unGAC the adapter"
		$GacUtilOutput = & "$($GacUtilDir.FullName)\gacutil.exe" "/u" "$BINDING_ASSEMBLY_NAME"
		Write-Output "gacutil.exe returned: $GacUtilOutput"
	}
	else{
		Write-Error "Did not find the correct gacutil.exe version!"
	}
}
	

# 1. First identify which version of BizTalk server is installed
$bizTalkRegistryPath = "HKLM:\SOFTWARE\Microsoft\BizTalk Server\3.0"

if ((Test-Path $bizTalkRegistryPath) -eq $true){
	 # Set location to BizTalk registry key
    # Push-Location $bizTalkRegistryPath
    $key = Get-Item $bizTalkRegistryPath
    # Get version number. TODO: Have to do a check to see if this is an array of keys or simply key
    [string]$productVersion = $key.GetValue("ProductVersion")
	
	# 2. Install the correct adapter as binding
	Write-Host "Product version is: $productVersion"

	# 2. Install the correct adapter as binding
	switch($productVersion){
		# BizTalk 2010
		{ $productVersion -match "3.9.*" }  { UninstallAdapter "2010" }
		# BizTalk 2013
		{ $productVersion -match "3.10.*"}  { UninstallAdapter "2013" }
		# BizTalk 2013R2
		{ $productVersion -match "3.11.*" } { UninstallAdapter "2013R2" }
		# BizTalk 2016
		{ $productVersion -match "3.12.*" } { UninstallAdapter "2016" }
		# deafult
		default { Write-Error "No suitable adapter version found for the currently installed BizTalk Server version";break }
		}
}
else
{
	Write-Error "Unable to find installed BizTalk Server instance!"		
}