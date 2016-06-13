@echo off
C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe TransMock.BTS2010.sln /nologo /t:Clean,Build /p:Configuration="Release" /p:Platform="Mixed Platforms"

C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe TransMock.BTS2013.sln /nologo /t:Clean,Build /p:Configuration="Release" /p:Platform="Mixed Platforms"

C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe TransMock.BTS2013R2.sln /nologo /t:Clean,Build /p:Configuration="Release" /p:Platform="Mixed Platforms"

pause