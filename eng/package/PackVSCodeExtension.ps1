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
    # writing back changes temporarily disabled until internal machines have access to pwsh.exe
    # see https://github.com/dotnet/core-eng/issues/9913
    #$packageJsonContents | ConvertTo-Json -depth 100 | Out-File $packageJsonPath

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
