@echo off
echo *** Building the Adapter ***
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Adapter\TransMock.Wcf.Adapter\TransMock.Wcf.Adapter.BTS2010.csproj /nologo /t:Rebuild /p:Configuration="Release"
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Adapter\TransMock.Wcf.Adapter\TransMock.Wcf.Adapter.BTS2013.csproj /nologo /t:Rebuild /p:Configuration="Release"
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Adapter\TransMock.Wcf.Adapter\TransMock.Wcf.Adapter.BTS2013R2.csproj /nologo /t:Rebuild /p:Configuration="Release"
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Adapter\TransMock.Wcf.Adapter\TransMock.Wcf.Adapter.BTS2016.csproj /nologo /t:Rebuild /p:Configuration="Release"

echo *** Building the Mockifier ***
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Mockifier\TransMock.Mockifier\TransMock.Mockifier.BTS2010.csproj /nologo /t:Rebuild /p:Configuration="Release"
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Mockifier\TransMock.Mockifier\TransMock.Mockifier.BTS2013.csproj /nologo /t:Rebuild /p:Configuration="Release"
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Mockifier\TransMock.Mockifier\TransMock.Mockifier.BTS2013R2.csproj /nologo /t:Rebuild /p:Configuration="Release"
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Mockifier\TransMock.Mockifier\TransMock.Mockifier.BTS2016.csproj /nologo /t:Rebuild /p:Configuration="Release"

echo *** Building the BizUnit Integration ***
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Integration\BizUnit\TransMock.Integration.BizUnit\TransMock.Integration.BizUnit.BTS2010.csproj /nologo /t:Rebuild /p:Configuration="Release"
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Integration\BizUnit\TransMock.Integration.BizUnit\TransMock.Integration.BizUnit.BTS2013.csproj /nologo /t:Rebuild /p:Configuration="Release"
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Integration\BizUnit\TransMock.Integration.BizUnit\TransMock.Integration.BizUnit.BTS2013R2.csproj /nologo /t:Rebuild /p:Configuration="Release"
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Integration\BizUnit\TransMock.Integration.BizUnit\TransMock.Integration.BizUnit.BTS2016.csproj /nologo /t:Rebuild /p:Configuration="Release"

echo *** Building the BizTalk test utils ***
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Test\TransMock.TestUtils.BizTalk\TransMock.TestUtils.BizTalk.BTS2010.csproj /nologo /t:Rebuild /p:Configuration="Release"
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Test\TransMock.TestUtils.BizTalk\TransMock.TestUtils.BizTalk.BTS2013.csproj /nologo /t:Rebuild /p:Configuration="Release"
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Test\TransMock.TestUtils.BizTalk\TransMock.TestUtils.BizTalk.BTS2013R2.csproj /nologo /t:Rebuild /p:Configuration="Release"
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Test\TransMock.TestUtils.BizTalk\TransMock.TestUtils.BizTalk.BTS2016.csproj /nologo /t:Rebuild /p:Configuration="Release"

echo *** Building the NugetPackages
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Deployment\TransMock.Deploy.Framework\TransMock.Deploy.Framework.nuproj /nologo /t:Build /p:Configuration="Release"
"C:\Program Files (x86)\MSBuild\14.0\Bin"\msbuild.exe .\Deployment\TransMock.Deploy.TestUtils\TransMock.Deploy.TestUtils.nuproj /nologo /t:Build /p:Configuration="Release"

pause