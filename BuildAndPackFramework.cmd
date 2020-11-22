@if "%1"=="" goto usage

:build
dotnet build Framework\TransMock\TransMock.csproj -c Release -p:Version=%1
dotnet build Mockifier\TransMock.Mockifier\TransMock.Mockifier.csproj -c Release -p:Version=%1
dotnet build Integration\BizUnit\TransMock.Integration.BizUnit\TransMock.Integration.BizUnit.csproj -c Release -p:Version=%1
@goto pack

:pack
call nuget pack Deployment\TransMock.Deploy.Framework\TransMock.Framework.nuspec -Version %1 -OutputDirectory build\packages
@goto end

:usage
 echo Usage: BuildAndPackAdapter version-number
 @goto end

:end