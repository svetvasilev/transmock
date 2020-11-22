@if "%1"=="" goto usage

:build
dotnet build Test\TransMock.TestUtils.BizTalk\TransMock.TestUtils.BizTalk.csproj -c Release -p:VersionPrefix=%1
@goto pack

:pack
call nuget pack Deployment\TransMock.Deploy.TestUtils\TransMock.TestUtils.BizTalk.nuspec -Symbols -Version %1 -OutputDirectory build\packages -SymbolPackageFormat snupkg
@goto end

:usage
 echo Usage: BuildAndPackAdapter version-number
 @goto end

:end