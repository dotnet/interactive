param (
    [string][Alias('version')]$sqlToolsVersion,
    [string][Alias('out')]$packageOutputDirectory
)

$githubReleasePackageName = "Microsoft.SqlTools.ServiceLayer"
$githubReleasePackageUri = "https://github.com/microsoft/sqltoolsservice/releases/download/"
$githubLicenseText = "https://raw.githubusercontent.com/microsoft/sqltoolsservice/main/license.txt"
$githubSqlToolsSdkIcon = "https://microsoft.github.io/sqltoolssdk/images/sqlserver.png"

function Create-Directory ([string[]] $path) {
    New-Item -Path $path -Force -ItemType 'Directory' | Out-Null
}

# This will exec a process using the console and return it's exit code.
# This will not throw when the process fails.
# Returns process exit code.
function Exec-Process([string]$command, [string]$commandArgs) {
  $startInfo = New-Object System.Diagnostics.ProcessStartInfo
  $startInfo.FileName = $command
  $startInfo.Arguments = $commandArgs
  $startInfo.UseShellExecute = $false
  $startInfo.WorkingDirectory = Get-Location

  $process = New-Object System.Diagnostics.Process
  $process.StartInfo = $startInfo
  $process.Start() | Out-Null

  $finished = $false
  try {
    while (-not $process.WaitForExit(100)) {
      # Non-blocking loop done to allow ctr-c interrupts
    }

    $finished = $true
    return $global:LASTEXITCODE = $process.ExitCode
  }
  finally {
    # If we didn't finish then an error occurred or the user hit ctrl-c.  Either
    # way kill the process
    if (-not $finished) {
      $process.Kill()
    }
  }
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
        Exec-Process "tar" "-zxf $tarfile  -C $packageDir"
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
