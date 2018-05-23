@echo off
echo *** Building the Adapter ***
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Adapter\TransMock.Wcf.Adapter.Tests\TransMock.Wcf.Adapter.Tests.BTS2010.csproj /nologo /t:Rebuild /p:Configuration="Debug"
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Adapter\TransMock.Wcf.Adapter.Tests\TransMock.Wcf.Adapter.Tests.BTS2013.csproj /nologo /t:Rebuild /p:Configuration="Debug"
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Adapter\TransMock.Wcf.Adapter.Tests\TransMock.Wcf.Adapter.Tests.BTS2013R2.csproj /nologo /t:Rebuild /p:Configuration="Debug"
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Adapter\TransMock.Wcf.Adapter.Tests\TransMock.Wcf.Adapter.Tests.BTS2016.csproj /nologo /t:Rebuild /p:Configuration="Debug"

echo *** Building the Mockifier ***
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Mockifier\TransMock.Mockifier.Tests\TransMock.Mockifier.Tests.BTS2010.csproj /nologo /t:Rebuild /p:Configuration="Debug"
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Mockifier\TransMock.Mockifier.Tests\TransMock.Mockifier.Tests.BTS2013.csproj /nologo /t:Rebuild /p:Configuration="Debug"
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Mockifier\TransMock.Mockifier.Tests\TransMock.Mockifier.Tests.BTS2013R2.csproj /nologo /t:Rebuild /p:Configuration="Debug"
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Mockifier\TransMock.Mockifier.Tests\TransMock.Mockifier.Tests.BTS2016.csproj /nologo /t:Rebuild /p:Configuration="Debug"

echo *** Building the BizUnit Integration ***
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Integration\BizUnit\TransMock.Integration.BizUnit.Tests\TransMock.Integration.BizUnit.Tests.BTS2010.csproj /nologo /t:Rebuild /p:Configuration="Debug"
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Integration\BizUnit\TransMock.Integration.BizUnit.Tests\TransMock.Integration.BizUnit.Tests.BTS2013.csproj /nologo /t:Rebuild /p:Configuration="Debug"
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Integration\BizUnit\TransMock.Integration.BizUnit.Tests\TransMock.Integration.BizUnit.Tests.BTS2013R2.csproj /nologo /t:Rebuild /p:Configuration="Debug"
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Integration\BizUnit\TransMock.Integration.BizUnit.Tests\TransMock.Integration.BizUnit.Tests.BTS2016.csproj /nologo /t:Rebuild /p:Configuration="Debug"

echo *** Building the BizTalk test utils ***
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Test\TransMock.TestUtils.BizTalk.Tests\TransMock.TestUtils.BizTalk.Tests.BTS2010.csproj /nologo /t:Rebuild /p:Configuration="Debug"
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Test\TransMock.TestUtils.BizTalk.Tests\TransMock.TestUtils.BizTalk.Tests.BTS2013.csproj /nologo /t:Rebuild /p:Configuration="Debug"
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Test\TransMock.TestUtils.BizTalk.Tests\TransMock.TestUtils.BizTalk.Tests.BTS2013R2.csproj /nologo /t:Rebuild /p:Configuration="Debug"
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Test\TransMock.TestUtils.BizTalk.Tests\TransMock.TestUtils.BizTalk.Tests.BTS2016.csproj /nologo /t:Rebuild /p:Configuration="Debug"

echo *** Building the NugetPackages
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Deployment\TransMock.Deploy.Framework\TransMock.Deploy.Framework.nuproj /nologo /t:Build /p:Configuration="Debug"
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Deployment\TransMock.Deploy.TestUtils\TransMock.Deploy.TestUtils.nuproj /nologo /t:Build /p:Configuration="Debug"

pause