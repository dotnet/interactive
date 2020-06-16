[CmdletBinding(PositionalBinding=$false)]
param (
    [string]$stableToolVersionNumber,
    [string]$gitSha,
    [string]$outDir
)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

try {
    # set version
    Write-Host "Setting package version to $stableToolVersionNumber"
    npm version $stableToolVersionNumber

    # append git sha to package description
    $packageJsonPath = Join-Path (Get-Location) "package.json"
    Write-Host "Appending git sha to description in $packageJsonPath"
    $packageJsonContents = (Get-Content $packageJsonPath | Out-String | ConvertFrom-Json)
    $packageJsonContents.description += "  Git SHA $gitSha"
    $packageJsonContents | ConvertTo-Json -depth 100 | Out-File $packageJsonPath

    # create destination
    New-Item -Path $outDir -ItemType Directory

    # copy publish scripts
    Copy-Item -Path $PSScriptRoot\..\publish\* -Destination $outDir -Recurse

    # pack
    Write-Host "Packing extension"
    npx vsce package --out "$outDir\dotnet-interactive-vscode-$stableToolVersionNumber.vsix"
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
