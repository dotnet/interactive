
# clean up the previously-cached NuGet packages
Remove-Item -Recurse ~\.nuget\packages\microsoft.dotnet.interactive*

# build and pack dotnet-interactive 
dotnet clean
dotnet build
dotnet pack  /p:PackageVersion=2.0.0

# copy the dotnet-interactive packages to the temp directory
Get-ChildItem -Recurse -Filter *.nupkg | Copy-Item -Destination c:\temp

# # build and pack nteract extension
# npm i C:\dev\dotnet-interactive-visualization\src\Microsoft.DotNet.Interactive.nteract.js\
# dotnet clean C:\dev\dotnet-interactive-visualization\
# dotnet build C:\dev\dotnet-interactive-visualization\
# dotnet pack C:\dev\dotnet-interactive-visualization\src\Microsoft.DotNet.Interactive.nteract.nuget  /p:PackageVersion=1.0.0

# # copy the nteract packages to the temp directory
# Get-ChildItem -Path C:\dev\dotnet-interactive-visualization\src\Microsoft.DotNet.Interactive.nteract.nuget\bin\Debug -Recurse -Filter *.nupkg | Copy-Item -Destination c:\temp