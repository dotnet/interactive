
# clean up the previously-cached NuGet packages
Remove-Item -Recurse ~\.nuget\packages\microsoft.dotnet.interactive* -Force

# build and pack dotnet-interactive 
dotnet clean -c debug
dotnet build -c debug
dotnet pack -c debug /p:PackageVersion=2.0.0

# copy the dotnet-interactive packages to the temp directory
$destinationPath = "C:\temp\packages"
if (-not (Test-Path -Path $destinationPath -PathType Container)) {
    New-Item -Path $destinationPath -ItemType Directory -Force
}
Get-ChildItem -Recurse -Filter *.nupkg | Move-Item -Destination $destinationPath -Force

# delete the #r nuget caches
if (Test-Path -Path ~\.packagemanagement\nuget\Cache -PathType Container) {
    Remove-Item -Recurse -Force ~\.packagemanagement\nuget\Cache
}

if (Test-Path -Path ~\.packagemanagement\nuget\Projects -PathType Container) {
    Remove-Item -Recurse -Force ~\.packagemanagement\nuget\Projects
}
