Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function ReadJson([string]$packageJsonPath) {
    Write-Host "Reading contents from $packageJsonPath"
    $packageJsonContents = (Get-Content $packageJsonPath | Out-String | ConvertFrom-Json)
    return $packageJsonContents
}

function SaveJson([string]$packageJsonPath, [PSCustomObject]$packageJsonContents) {
    Write-Host "Writing contents to $packageJsonPath"
    $packageJsonContents | ConvertTo-Json -depth 100 | Out-File $packageJsonPath
}

function SetNpmVersionNumber([PSCustomObject]$packageJsonContents, [string]$packageVersionNumber) {
    Write-Host "Setting package version to $packageVersionNumber."
    $packageJsonContents.version = $packageVersionNumber
}

function EnsureCleanDirectory([string]$location) {
    if (Test-Path $location) {
        Remove-Item -Path $location -Recurse
    }

    New-Item -Path $location -ItemType Directory
}
