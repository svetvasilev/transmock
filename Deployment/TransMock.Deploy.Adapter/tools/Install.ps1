# Script for installing the TransMock Adapter binding as BizTalk adapter
param($installPath, $toolsPath, $package, $project)

Add-Type -AssemblyName "System.Configuration"
Add-Type -AssemblyName "System.ServiceModel"

$DebugPreference = "Continue"

Write-Debug "installPath parameter is: $installPath"
Write-Debug "toolsPath parameter is: $toolsPath"
Write-Debug "package parameter is: $package"
Write-Debug "project parameter is: $project"

# Cosntants
$BINDING_ASSEMBLY_NAME = "TransMock.Wcf.Adapter.dll"
$BINDINGELEM_NAME = "mockTransport"
$BINDINGELEM_TYPE = "TransMock.Wcf.Adapter.MockAdapterBindingElementExtensionElement"
$BINDING_NAME = "mockBinding"
$BINDING_TYPE = "TransMock.Wcf.Adapter.MockAdapterBindingCollectionElement"
$BINDING_SCHEME = "mock"

function AddMachineConfigurationInfo{
	param(
		[System.Configuration.Configuration] $Config,
		[string]$TargetPath)

	Write-Debug "AddMachineConfigurationInfo called with: $Config"
        
    $AssemblyPath = "$TargetPath\$BINDING_ASSEMBLY_NAME"
    [System.Reflection.Assembly]$AdapterAssembly = [System.Reflection.Assembly]::LoadFrom($AssemblyPath)

    Write-Debug "Adapter assembly loaded is: $($AdapterAssembly.FullName)"

    [System.Type]$BindingSectionType = $AdapterAssembly.GetType($BINDING_TYPE, $true)
            
	Write-Debug "Binding section type loaded is: $($BindingSectionType.FullName)"

    [System.Type]$BindingElementExtensionType = $AdapterAssembly.GetType($BINDINGELEM_TYPE, $true)
            
	Write-Debug "Binding element extention type loaded is: $($BindingElementExtensionType.FullName)"

    # add <client><endpoint>             
    $SectionGroup = $Config.GetSectionGroup("system.serviceModel") -as [System.ServiceModel.Configuration.ServiceModelSectionGroup]
            
	if ($SectionGroup)
    {
		Write-Output "serviceModel section group found. Continuing processing" -Foreground Cyan
        
        # [System.Configuration.ClientSection]$ClientSection = $SectionGroup.Client
        # foreach (ChannelEndpointElement elem in clientSection.Endpoints)
		Write-Debug "SectionGroup is: $SectionGroup"
		Write-Debug "Client is: $($SectionGroup.Client)"
		Write-Debug "Client.Endpoints is: $($SectionGroup.Client.Endpoints)"

        # add <bindingElementExtension>
        if (!$SectionGroup.Extensions.BindingElementExtensions.ContainsKey($BINDINGELEM_NAME))
        {
			Write-Output "Adding bindingElementExtention for : $BINDINGELEM_NAME" -Foreground Blue

            $ExtensionElement = New-Object "System.ServiceModel.Configuration.ExtensionElement" -ArgumentList `
				$BINDINGELEM_NAME,"$($BindingElementExtensionType.FullName), $($BindingElementExtensionType.Assembly.FullName)"
            
			$SectionGroup.Extensions.BindingElementExtensions.Add($ExtensionElement)

			Write-Output "Added bindingElementExtention for : $BINDINGELEM_NAME" -Foreground DarkGreen
        }

        # add <bindingExtension>
        if (!$SectionGroup.Extensions.BindingExtensions.ContainsKey($BINDING_NAME))
        {
			Write-Output "Adding bindingExtension for : $BINDING_NAME" -Foreground Blue

            $BindingExtension = New-Object "System.ServiceModel.Configuration.ExtensionElement" -ArgumentList `
				$BINDING_NAME, "$($BindingSectionType.FullName), $($BindingSectionType.Assembly.FullName)"
            
			$SectionGroup.Extensions.BindingExtensions.Add($BindingExtension)

			Write-Output "Added bindingExtension for : $BINDING_NAME" -Foreground DarkGreen
        }

		# add the client endpoint for the mock adapter
		$ChannelEndpointElementExists = $false
        # this call can throw an exception if there is problem 
        # loading endpoint configurations - e.g. each endpoint
        # tries to load binding which in turn loads the DLL
		if($SectionGroup.Client.Endpoints)
		{
			$SectionGroup.Client.Endpoints | % {
				if ($_.Binding.Equals($BINDING_NAME, [System.StringComparison]::OrdinalIgnoreCase) -and
					$_.Name.Equals($BINDING_SCHEME, [System.StringComparison]::OrdinalIgnoreCase) -and
					$_.Contract.Equals("IMetadataExchange", [System.StringComparison]::OrdinalIgnoreCase))
				{
					$ChannelEndpointElementExists = $true
					# break
				}
			}
		}
		
		if (!$ChannelEndpointElementExists)
		{
			Write-Output "Adding ChannelEndpointElement for : $BINDING_NAME" -Foreground Blue

			$EndpointElement = New-Object "System.ServiceModel.Configuration.ChannelEndpointElement"
			$EndpointElement.Binding = $BINDING_NAME
			$EndpointElement.Name = $BINDING_SCHEME
			$EndpointElement.Contract = "IMetadataExchange"
            
			$SectionGroup.Client.Endpoints.Add($EndpointElement)

			Write-Output "Added ChannelEndpointElement for : $BINDING_NAME" -Foreground DarkGreen
		}		

        $Config.Save()

		Write-Output "Successfully registered the TransMock adapter for BizTalk" -Foreground DarkGreen
    }
    else {
        Write-Error "Machine.Config doesn't contain system.serviceModel node"
	}
}

function InstallAdapter{
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
	$TargetDirectory = Get-ChildItem "$installPath\lib" -Filter "*$TargetNetVersion*" -Recurse -Directory
	if($TargetDirectory)
	{
		Write-Host "TargetDirectory is: $($TargetDirectory.FullName)"

		[System.Configuration.Configuration]$ConfigFile = [System.Configuration.ConfigurationManager]::OpenMachineConfiguration()
			
		AddMachineConfigurationInfo $ConfigFile $TargetDirectory.FullName
			
		# Perform the same operation for the 64-bit framework versions
		if([System.Environment]::Is64BitOperatingSystem -eq $true)
		{
			Write-Output "Adding the binding to the 64-bit machine.config file."
				
			$MachineConfigPathFor64Bit = [System.Runtime.InteropServices.RuntimeEnvironment]::
                    GetRuntimeDirectory().Replace("Framework", "Framework64");

            [System.Configuration.ConfigurationFileMap]$ConfigMap = New-Object "System.Configuration.ConfigurationFileMap" `
                -ArgumentList "$($MachineConfigPathFor64Bit)Config\machine.config"

            $ConfigFile = [System.Configuration.ConfigurationManager]::OpenMappedMachineConfiguration($ConfigMap)
                
			Write-Debug "Machine.Config for 64-bit returned is loaded: $ConfigFile"

			AddMachineConfigurationInfo $ConfigFile $TargetDirectory.FullName
		}
			
		# GAC the adapter
		# First we need to find gacutil
		$WinSDKDirectory = "C:\Program Files\Microsoft SDKs\Windows"
		$DependencyAssemblies = @("TransMock.Wcf.Adapter.Utils.dll","TransMock.Communication.NamedPipes.dll")

		if([System.Environment]::Is64BitOperatingSystem -eq $true){
			$WinSDKDirectory = $WinSDKDirectory -replace "Program Files","Program Files (x86)"
		}
			
		$GacUtilDir = Get-ChildItem $WinSDKDirectory -Filter "*$WinSDKFilter*" -Recurse -Directory | `
			Select-Object -Last 1
			
		if($GacUtilDir){
			Write-Debug "GacUtilDir found and is: $($GacUtilDir.FullName)"

			Write-Output "Invoking gacutil.exe to GAC the dependency assemblies"

			$DependencyAssemblies | % {
				Write-Output "GACing assembly $_"
				$GacUtilOutput = & "$($GacUtilDir.FullName)\gacutil.exe" "/i" "$($TargetDirectory.FullName)\$_"
				Write-Output "gacutil.exe returned: $GacUtilOutput"
			}

			Write-Output "Invoking gacutil.exe to GAC the adapter"
			$GacUtilOutput = & "$($GacUtilDir.FullName)\gacutil.exe" "/i" "$($TargetDirectory.FullName)\$BINDING_ASSEMBLY_NAME"
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
    # Push-Location $bizTalkRegistryPath
    $key = Get-Item $bizTalkRegistryPath
    # Get version number. TODO: Have to do a check to see if this is an array of keys or simply key
    [string]$productVersion = $key.GetValue("ProductVersion")
	
	# Pop-Location

	Write-Host "Product version is: $productVersion"

	# 2. Install the correct adapter as binding
	switch($productVersion){
		# BizTalk 2010
		{ $productVersion -match "3.9.*" }  { InstallAdapter "2010" }
		# BizTalk 2013
		{ $productVersion -match "3.10.*"}  { InstallAdapter "2013" }
		# BizTalk 2013R2
		{ $productVersion -match "3.11.*" } { InstallAdapter "2013R2" }
		# BizTalk 2016
		{ $productVersion -match "3.12.*" } { InstallAdapter "2016" }
		# BizTalk 2020
		{ $productVersion -match "3.13.*" } { InstallAdapter "2020" }
		# deafult
		default { Write-Error "No suitable adapter version found for the currently installed BizTalk Server version";break }
		}
	
}	
else
{
	Write-Error "Unable to find installed BizTalk Server instance!"		
}