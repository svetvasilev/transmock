@if "%1"=="" goto usage

:build
dotnet build Adapter\TransMock.Wcf.Adapter\TransMock.Wcf.Adapter.csproj -c Release -p:Version=%1
@goto pack

:pack
call nuget pack Deployment\TransMock.Deploy.Adapter\TransMock.Adapter.nuspec -Version %1 -OutputDirectory build\packages
@goto end

:usage
 echo Usage: BuildAndPackAdapter version-number
 @goto end

:end