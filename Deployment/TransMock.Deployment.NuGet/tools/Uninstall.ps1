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
		Write-Output "serviceModel section group found. Continuing processing" -Foreground Cyan
        
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


# 1. First identify which version of BizTalk server is installed
$bizTalkRegistryPath = "HKLM:\SOFTWARE\Microsoft\BizTalk Server"

if ((Test-Path $bizTalkRegistryPath) -eq $true){
	 # Set location to BizTalk registry key
    Push-Location $bizTalkRegistryPath
    $key = Get-ChildItem
    # Get version number. TODO: Have to do a check to see if this is an array of keys or simply key
    [string]$productVersion = $key[0].GetValue("ProductVersion")
	
	# 2. Install the correct adapter as binding
	if ($productVersion -eq "3.12.774.0")
	{	
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
		$CommsAssembly = "TransMock.Communication.NamedPipes"

		if([System.Environment]::Is64BitOperatingSystem -eq $true){
			$WinSDKDirectory = $WinSDKDirectory -replace "Program Files","Program Files (x86)"
		}
			
		$GacUtilDir = Get-ChildItem $WinSDKDirectory -Filter "*NETFX 4.6 Tools*" -Recurse -Directory
			
		if($GacUtilDir){
			Write-Debug "GacUtilDir found and is: $($GacUtilDir.FullName)"

			Write-Output "Invoking gacutil.exe to unGAC the communications assembly"
			$GacUtilOutput = & "$($GacUtilDir.FullName)\gacutil.exe" "/u" "$CommsAssembly"
			Write-Output "gacutil.exe returned: $GacUtilOutput"

			Write-Output "Invoking gacutil.exe to unGAC the adapter"
			$GacUtilOutput = & "$($GacUtilDir.FullName)\gacutil.exe" "/u" "$BINDING_ASSEMBLY_NAME"
			Write-Output "gacutil.exe returned: $GacUtilOutput"
		}
		else{
			Write-Error "Did not find the correct gacutil.exe version!"
		}		
		
	}

	Pop-Location
	
}