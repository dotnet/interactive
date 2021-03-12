[CmdletBinding(PositionalBinding = $false)]
param (
    [string]$packageVersionNumber,
    [string]$gitSha,
    [string]$outDir
)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function Build-NpmPackage() {
    $packageJsonPath = Join-Path (Get-Location) "package.json"
    $packageJsonContents = ReadJson -packageJsonPath $packageJsonPath
    SetNpmVersionNumber -packageJsonContents $packageJsonContents -packageVersionNumber $packageVersionNumber
    AddGitShaToDescription -packageJsonContents $packageJsonContents -gitSha $gitSha
    SaveJson -packageJsonPath $packagejsonPath -packageJsonContents $packageJsonContents

    # pack
    Write-Host "Packing package"
    npm pack
    Copy-Item -Path (Join-Path (Get-Location) "dotnet-interactive-$packageVersionNumber.tgz") -Destination $outDir
}

try {
    . "$PSScriptRoot\PackUtilities.ps1"

    # create destination
    EnsureCleanDirectory -location $outDir

    Build-NpmPackage
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
