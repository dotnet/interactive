[CmdletBinding(PositionalBinding = $false)]
param (
    [string]$stableToolVersionNumber,
    [string]$gitSha,
    [string]$outDir
)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function Build-Extension([string] $packageDirectory, [string] $packageVersionNumber, [string] $kernelVersionNumber) {
    Push-Location $packageDirectory

    # get JSON model for package.json
    Write-Host "Getting JSON package model"
    $packageJsonPath = Join-Path (Get-Location) "package.json"
    $packageJsonContents = (Get-Content $packageJsonPath | Out-String | ConvertFrom-Json)

    # set package version
    Write-Host "Setting package version to $packageVersionNumber"
    $packageJsonContents.version = $packageVersionNumber

    # set tool version
    Write-Host "Setting tool version to $kernelVersionNumber"
    $packageJsonContents.contributes.configuration.properties."dotnet-interactive.minimumInteractiveToolVersion"."default" = $kernelVersionNumber

    # append git sha to package description
    Write-Host "Appending git sha to description in $packageJsonPath"
    $packageJsonContents.description += "  Git SHA $gitSha"

    # write out updated JSON
    Write-Host "Writing updated package.json"
    $packageJsonContents | ConvertTo-Json -depth 100 | Out-File $packageJsonPath

    # create destination
    New-Item -Path "$outDir\$packageDirectory" -ItemType Directory

    # copy publish scripts
    Copy-Item -Path $PSScriptRoot\..\publish\* -Destination $outDir -Recurse

    # pack
    Write-Host "Packing extension"
    npx vsce package --out "$outDir\$packageDirectory\dotnet-interactive-vscode-$packageVersionNumber.vsix"

    Pop-Location
}

try {
    $stablePackageVersion = "${stableToolVersionNumber}00"
    $insidersPackageVersion = "${stableToolVersionNumber}01"
    Build-Extension -packageDirectory "stable" -packageVersionNumber $stablePackageVersion -kernelVersionNumber $stableToolVersionNumber
    Build-Extension -packageDirectory "insiders" -packageVersionNumber $insidersPackageVersion -kernelVersionNumber $stableToolVersionNumber
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
