param (
    [string][Alias('version')]$sqlToolsVersion,
    [string][Alias('out')]$packageOutputDirectory
)

$githubReleasePackageName = "Microsoft.SqlTools.ServiceLayer"
$githubReleasePackageUri = "https://github.com/microsoft/sqltoolsservice/releases/download/"
$githubLicenseText = "https://raw.githubusercontent.com/microsoft/sqltoolsservice/main/license.txt"
$githubSqlToolsSdkIcon = "https://microsoft.github.io/sqltoolssdk/images/sqlserver.png"

$sqlVersion="v$version"
$downloads=$out

function Create-Directory ([string[]] $path) {
    New-Item -Path $path -Force -ItemType 'Directory' | Out-Null
}

function Unzip([string]$zipfile, [string]$outpath) {
  Add-Type -AssemblyName System.IO.Compression.FileSystem
  [System.IO.Compression.ZipFile]::ExtractToDirectory($zipfile, $outpath)
}

Function DeGZip-File{
    Param($infile, $outfile = ($infile -replace '\.gz$',''))

    $input = New-Object System.IO.FileStream $inFile, ([IO.FileMode]::Open), ([IO.FileAccess]::Read), ([IO.FileShare]::Read)
    $output = New-Object System.IO.FileStream $outFile, ([IO.FileMode]::Create), ([IO.FileAccess]::Write), ([IO.FileShare]::None)
    $gzipStream = New-Object System.IO.Compression.GzipStream $input, ([IO.Compression.CompressionMode]::Decompress)
    $buffer = New-Object byte[](1024)
    while($true){
        $read = $gzipstream.Read($buffer, 0, 1024)
        if ($read -le 0){break}
        $output.Write($buffer, 0, $read)
        }
    $gzipStream.Close()
    $output.Close()
    $input.Close()
}

function DownloadPackageFromGithub {
    Param ($rootdir, $filename, $uri)

    Write-Host "DownloadPackageFromGithub:   ($rootdir, $filename, $uri)"
    $isTarGz = $filename.EndsWith(".tar.gz")
    Create-Directory $rootdir

    $workdir = [System.IO.Path]::GetTempPath()
    Create-Directory $workdir

    $dlfile = Join-Path $workdir $filename

    <# Download the package from github #>
    Invoke-WebRequest $uri -OutFile $dlfile

    <# Unzip or Untar the package #>
    if ($isTarGz) {
        $barename = $filename.Replace(".tar.gz", "")
        $packageDir = Join-Path "$rootdir"  "$barename"
        Create-Directory $packageDir
        $tarfile = $dlfile + ".tar"
        DeGZip-File $dlFile $tarfile
        tar -xf $tarfile  -C $packageDir
    }
    else {
        $packageDir = Join-Path "$rootdir"  ($filename -replace ".zip", "")
        Create-Directory $packageDir	
        Unzip $dlFile $packageDir  #>
    }
    try {
        [System.IO.Directory]::Delete($workdir, $true)
    }
    catch {
    }
}

function DownloadPackagesFromGithub {
    Param ($basename, $version, $uribase, $rootdir)
    Write-Host "DownloadPackagesFromGithub: ($basename, $version, $uribase, $rootdir)"
    try { [System.IO.Directory]::Delete($rootdir, $true) } catch {}
    try { Create-Directory $rootdir } catch {}

    $licenseFile = Join-Path "$rootdir"  "license.txt"
    $sdkIconFile = Join-Path "$rootdir"  "sqlserver.png"
    Invoke-WebRequest $githubLicenseText -OutFile $licenseFile
    Invoke-WebRequest $githubSqlToolsSdkIcon -Outfile $sdkIconFile

    $tarext = ".tar.gz"
    $zipext = ".zip"
    $netcoreapp31tfm = "netcoreapp3.1"

    <# Download .tar.gz files #>
    "osx-x64", "rhel-x64" | ForEach-Object {
        $packagename = $basename + "-" + $_ + "-" + $netcoreapp31tfm + $tarext
        DownloadPackageFromGithub $rootdir $packagename ($uribase + $version + "/" + $packagename)
    }

    <# Download .zip files #>
    "win-x86", "win-x64", "win10-arm", "win10-arm64" | ForEach-Object {
        $packagename = $basename + "-" + $_ + "-" + $netcoreapp31tfm + $zipext
        DownloadPackageFromGithub $rootdir $packagename ($uribase + $version + "/" + $packagename)
    }
}

DownloadPackagesFromGithub $githubReleasePackageName $sqlToolsVersion $githubReleasePackageUri $packageOutputDirectory

$outputPath=(Join-Path $PSScriptRoot "..\artifacts\packages\Release\Shipping")
New-Item -Path $outputPath -ItemType Directory -Force

$projRoot=(Join-Path $PSScriptRoot "..\src\Microsoft.SqlToolsService")

dotnet pack "$projRoot/runtime.osx-x64.native.Microsoft.SqlToolsService/runtime.osx-x64.native.Microsoft.SqlToolsService.csproj" /p:SqlToolsVersion=$sqlVersion --configuration Release -o $outputPath
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet pack "$projRoot/runtime.rhel-x64.native.Microsoft.SqlToolsService/runtime.rhel-x64.native.Microsoft.SqlToolsService.csproj" /p:SqlToolsVersion=$sqlVersion --configuration Release -o $outputPath
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet pack "$projRoot/runtime.win-x64.native.Microsoft.SqlToolsService/runtime.win-x64.native.Microsoft.SqlToolsService.csproj" /p:SqlToolsVersion=$sqlVersion --configuration Release -o $outputPath
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet pack "$projRoot/runtime.win-x86.native.Microsoft.SqlToolsService/runtime.win-x86.native.Microsoft.SqlToolsService.csproj" /p:SqlToolsVersion=$sqlVersion --configuration Release -o $outputPath
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet pack "$projRoot\runtime.win10-arm.native.Microsoft.SqlToolsService\runtime.win10-arm.native.Microsoft.SqlToolsService.csproj" /p:SqlToolsVersion=$sqlVersion --configuration Release -o $outputPath
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet pack "$projRoot\runtime.win10-arm64.native.Microsoft.SqlToolsService\runtime.win10-arm64.native.Microsoft.SqlToolsService.csproj" /p:SqlToolsVersion=$sqlVersion --configuration Release -o $outputPath
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet pack "$projRoot\Microsoft.SqlToolsService.csproj" /p:SqlToolsVersion=$sqlVersion --configuration Release -o $outputPath
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }


