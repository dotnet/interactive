@echo off

set version=3.0.0-release.52
powershell -noprofile -executionPolicy RemoteSigned -file "%~dp0eng\DownLoadSqlToolsService.ps1" -out %~dp0artifacts\downloads -version v%version% %* 

set ProjRoot="%~dp0src\Microsoft.SqlToolsService"

set outputPath=%~dp0artifacts\packages\Release\Shipping
md "%outputPath%"
echo %outputPath%

dotnet pack "%ProjRoot%\runtime.osx-x64.native.Microsoft.SqlToolsService\runtime.osx-x64.native.Microsoft.SqlToolsService.csproj" /p:SqlToolsVersion=%version% --configuration Release -o %outputPath%
dotnet pack "%ProjRoot%\runtime.rhel-x64.native.Microsoft.SqlToolsService\runtime.rhel-x64.native.Microsoft.SqlToolsService.csproj" /p:SqlToolsVersion=%version% --configuration Release -o %outputPath%
dotnet pack "%ProjRoot%\runtime.win-x64.native.Microsoft.SqlToolsService\runtime.win-x64.native.Microsoft.SqlToolsService.csproj" /p:SqlToolsVersion=%version% --configuration Release -o %outputPath%
dotnet pack "%ProjRoot%\runtime.win-x86.native.Microsoft.SqlToolsService\runtime.win-x86.native.Microsoft.SqlToolsService.csproj" /p:SqlToolsVersion=%version% --configuration Release -o %outputPath%
dotnet pack "%ProjRoot%\runtime.win10-arm.native.Microsoft.SqlToolsService\runtime.win10-arm.native.Microsoft.SqlToolsService.csproj" /p:SqlToolsVersion=%version% --configuration Release -o %outputPath%
dotnet pack "%ProjRoot%\runtime.win10-arm64.native.Microsoft.SqlToolsService\runtime.win10-arm64.native.Microsoft.SqlToolsService.csproj" /p:SqlToolsVersion=%version% --configuration Release -o %outputPath%
dotnet pack "%ProjRoot%\Microsoft.SqlToolsService.csproj" /p:SqlToolsVersion=%version% --configuration Release -o %outputPath%

echo done