@if "%2"=="" goto usage
@if "%2"=="" goto usage

:build
dotnet build Test\TransMock.TestUtils.BizTalk\TransMock.TestUtils.BizTalk.csproj -c %1 -p:Version=%2
@goto pack

:pack
call nuget pack Deployment\TransMock.Deploy.TestUtils\TransMock.TestUtils.BizTalk.nuspec -Version %2 -OutputDirectory build\packages
@goto end

:usage
 echo Usage: BuildAndPackAdapter configuration version-number
 @goto end

:end