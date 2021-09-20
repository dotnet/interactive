
# clean up the previously-cached NuGet packages
Remove-Item -Recurse ~\.nuget\packages\microsoft.dotnet.interactive* -Force

# build and pack dotnet-interactive 
dotnet clean
dotnet pack  /p:PackageVersion=2.0.0

# copy the dotnet-interactive packages to the temp directory
Get-ChildItem -Recurse -Filter *.nupkg | Copy-Item -Destination c:\temp -Force

