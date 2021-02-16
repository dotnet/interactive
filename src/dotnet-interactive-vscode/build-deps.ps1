# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function Get-Integrity([string] $filePath) {
    $hash = (Get-FileHash $filePath -Algorithm SHA512).Hash
    $bytes = (0..($hash.Length / 2)) | ForEach-Object { [System.Convert]::ToByte($hash.Substring($_, 2), 16) }
    return [System.Convert]::ToBase64String($bytes)
}

function Update-PackageSha([string] $projectJsonLockPath, [string] $packageName, [string] $packageTarball) {
    $packageJsonLockContents = Get-Content $projectJsonLockPath | Out-String | ConvertFrom-Json
    # TEMP: Don't compute integrity hash, just delete it
    # $hash = Get-Integrity -filePath $packageTarball
    # $packageJsonLockContents."dependencies"."$packageName"."integrity" = "sha512-$hash"
    $packageJsonLockContents."dependencies"."$packageName".PSObject.Properties.Remove("integrity")
    $packageJsonLockContents | ConvertTo-Json -depth 100 | Out-File $projectJsonLockPath
}

try {
    $interfacesTarball = "$PSScriptRoot/src/interfaces/vscode-interfaces-1.0.0.tgz"
    $insidersTarball = "$PSScriptRoot/src/vscode/insiders/vscode-insiders-1.0.0.tgz"
    $stableTarball = "$PSScriptRoot/src/vscode/stable/vscode-stable-1.0.0.tgz"

    # Build-Interfaces
    Push-Location "$PSScriptRoot/src/interfaces"
    npm install; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    npm run compile; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    npm pack; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Pop-Location

    # Build insiders
    Update-PackageSha -projectJsonLockPath "$PSScriptRoot/src/vscode/insiders/package-lock.json" -packageName "vscode-interfaces" -packageTarball $interfacesTarball
    Push-Location "$PSScriptRoot/src/vscode/insiders"
    npm install; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    npm run compile; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    npm pack; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Pop-Location

    # Build stable
    Update-PackageSha -projectJsonLockPath "$PSScriptRoot/src/vscode/stable/package-lock.json" -packageName "vscode-interfaces" -packageTarball $interfacesTarball
    Push-Location "$PSScriptRoot/src/vscode/stable"
    npm install; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    npm run compile; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    npm pack; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Pop-Location

    # Root project
    Update-PackageSha -projectJsonLockPath "$PSScriptRoot/package-lock.json" -packageName "vscode-interfaces" -packageTarball $interfacesTarball
    Update-PackageSha -projectJsonLockPath "$PSScriptRoot/package-lock.json" -packageName "vscode-insiders" -packageTarball $insidersTarball
    Update-PackageSha -projectJsonLockPath "$PSScriptRoot/package-lock.json" -packageName "vscode-stable" -packageTarball $stableTarball
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
