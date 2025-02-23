
Push-Location $PSScriptRoot
$ErrorActionPreference = "Stop"

try
{
    # clean up the previously-cached NuGet packages
    $nugetCachePath = $env:NUGET_HTTP_CACHE_PATH
    if (-not $nugetCachePath) {
        $nugetCachePath = "~\.nuget\packages"
    }
    Remove-Item -Recurse "$nugetCachePath\microsoft.dotnet.interactive*" -Force

    # build and pack dotnet-interactive 
    dotnet clean -c debug
    # dotnet build -c debug /p:Version=2.0.0
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
}
finally
{
    $PopLocation
}
