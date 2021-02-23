@if "%1"=="" goto usage
@if "%2"=="" goto usage

:build
dotnet build Framework\TransMock\TransMock.csproj -c %1 -p:Version=%2
dotnet build Mockifier\TransMock.Mockifier\TransMock.Mockifier.csproj -c %1 -p:Version=%2
dotnet build Integration\BizUnit\TransMock.Integration.BizUnit\TransMock.Integration.BizUnit.csproj -c %1 -p:Version=%2
@goto pack

:pack
call nuget pack Deployment\TransMock.Deploy.Framework\TransMock.Framework.nuspec -Version %2 -OutputDirectory build\packages
@goto end

:usage
 echo Usage: BuildAndPackAdapter configuration version-number
 @goto end

:end