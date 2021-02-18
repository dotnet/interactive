# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function Clear-PackageSha([string] $projectJsonLockPath, [string] $packageName) {
    $packageJsonLockContents = Get-Content $projectJsonLockPath | Out-String | ConvertFrom-Json
    $packageJsonLockContents."dependencies"."$packageName".PSObject.Properties.Remove("integrity")
    $packageJsonLockContents | ConvertTo-Json -depth 100 | Out-File $projectJsonLockPath
}

try {
    Clear-PackageSha -projectJsonLockPath "$PSScriptRoot/src/vscode/insiders/package-lock.json" -packageName "dotnet-interactive-vscode-interfaces"
    Clear-PackageSha -projectJsonLockPath "$PSScriptRoot/src/vscode/stable/package-lock.json" -packageName "dotnet-interactive-vscode-interfaces"
    Clear-PackageSha -projectJsonLockPath "$PSScriptRoot/package-lock.json" -packageName "dotnet-interactive-vscode-interfaces"
    Clear-PackageSha -projectJsonLockPath "$PSScriptRoot/package-lock.json" -packageName "dotnet-interactive-vscode-insiders"
    Clear-PackageSha -projectJsonLockPath "$PSScriptRoot/package-lock.json" -packageName "dotnet-interactive-vscode-stable"
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
