@echo off

set version=3.0.0-release.52
rem powershell -noprofile -executionPolicy RemoteSigned -file "%~dp0eng\DownLoadSqlToolsServiceLayerPackage.ps1" -out %~dp0artifacts\downloads -version v%version% %* 

set ProjRoot="%~dp0src\Microsoft.SqlTools.ServiceLayer"

set outputPath=%~dp0artifacts\packages\Release\Shipping
md "%outputPath%"
echo %outputPath%

dotnet pack "%ProjRoot%\runtime.osx-x64.runtime.native.Microsoft.SqlTools.ServiceLayer\runtime.osx-x64.runtime.native.Microsoft.SqlTools.ServiceLayer.csproj" /p:SqlToolsVersion=%version% --configuration Release -o %outputPath%
dotnet pack "%ProjRoot%\runtime.rhel-x64.runtime.native.Microsoft.SqlTools.ServiceLayer\runtime.rhel-x64.runtime.native.Microsoft.SqlTools.ServiceLayer.csproj" /p:SqlToolsVersion=%version% --configuration Release -o %outputPath%
dotnet pack "%ProjRoot%\runtime.win-x64.runtime.native.Microsoft.SqlTools.ServiceLayer\runtime.win-x64.runtime.native.Microsoft.SqlTools.ServiceLayer.csproj" /p:SqlToolsVersion=%version% --configuration Release -o %outputPath%
dotnet pack "%ProjRoot%\runtime.win-x86.runtime.native.Microsoft.SqlTools.ServiceLayer\runtime.win-x86.runtime.native.Microsoft.SqlTools.ServiceLayer.csproj" /p:SqlToolsVersion=%version% --configuration Release -o %outputPath%
dotnet pack "%ProjRoot%\runtime.win10-arm.runtime.native.Microsoft.SqlTools.ServiceLayer\runtime.win10-arm.runtime.native.Microsoft.SqlTools.ServiceLayer.csproj" /p:SqlToolsVersion=%version% --configuration Release -o %outputPath%
dotnet pack "%ProjRoot%\runtime.win10-arm64.runtime.native.Microsoft.SqlTools.ServiceLayer\runtime.win10-arm64.runtime.native.Microsoft.SqlTools.ServiceLayer.csproj" /p:SqlToolsVersion=%version% --configuration Release -o %outputPath%
dotnet pack "%ProjRoot%\Microsoft.SqlTools.ServiceLayer.csproj" /p:SqlToolsVersion=%version% --configuration Release -o %outputPath%

echo done