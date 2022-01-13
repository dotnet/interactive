[CmdletBinding(PositionalBinding = $false)]
param (
    [string]$stableToolVersionNumber,
    [string]$gitSha,
    [string]$outDir
)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function Build-VsCodeExtension([string] $packageDirectory, [string] $outputSubDirectory, [string] $packageVersionNumber, [string] $kernelVersionNumber = "") {
    Push-Location $packageDirectory

    $packageJsonPath = Join-Path (Get-Location) "package.json"
    $packageJsonContents = ReadJson -packageJsonPath $packageJsonPath
    SetNpmVersionNumber -packageJsonContents $packageJsonContents -packageVersionNumber $packageVersionNumber
    AddGitShaToDescription -packageJsonContents $packageJsonContents -gitSha $gitSha

    # set tool version
    if ($kernelVersionNumber -Ne "") {
        Write-Host "Setting tool version to $kernelVersionNumber"
        $packageJsonContents.contributes.configuration.properties."dotnet-interactive.minimumInteractiveToolVersion"."default" = $kernelVersionNumber
    }

    SaveJson -packageJsonPath $packagejsonPath -packageJsonContents $packageJsonContents

    # create destination
    if ($outputSubDirectory -Eq "") {
        $outputSubDirectory = $packageDirectory
    }
    EnsureCleanDirectory -location "$outDir\$outputSubDirectory"

    # pack
    Write-Host "Packing extension"
    npx vsce package --out "$outDir\$outputSubDirectory\dotnet-interactive-vscode-$packageVersionNumber.vsix"

    Pop-Location
}

try {
    . "$PSScriptRoot\PackUtilities.ps1"

    # copy publish scripts
    EnsureCleanDirectory -location $outDir
    Copy-Item -Path $PSScriptRoot\..\publish\* -Destination $outDir -Recurse

    $stablePackageVersion = "${stableToolVersionNumber}0"
    $insidersPackageVersion = "${stableToolVersionNumber}1"
    Build-VsCodeExtension -packageDirectory "dotnet-interactive-vscode" -outputSubDirectory "stable-locked" -packageVersionNumber $stablePackageVersion
    Build-VsCodeExtension -packageDirectory "dotnet-interactive-vscode" -outputSubDirectory "stable" -packageVersionNumber $stablePackageVersion -kernelVersionNumber $stableToolVersionNumber
    Build-VsCodeExtension -packageDirectory "dotnet-interactive-vscode-insiders" -outputSubDirectory "insiders" -packageVersionNumber $insidersPackageVersion -kernelVersionNumber $stableToolVersionNumber
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
