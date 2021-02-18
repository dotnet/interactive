# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

try {
    . "$PSScriptRoot/Clear-PackageSha.ps1" -projectJsonLockPath "$PSScriptRoot/package-lock.json" -packageName "vscode-interfaces"; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    . "$PSScriptRoot/Clear-PackageSha.ps1" -projectJsonLockPath "$PSScriptRoot/package-lock.json" -packageName "vscode-insiders"; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    . "$PSScriptRoot/Clear-PackageSha.ps1" -projectJsonLockPath "$PSScriptRoot/package-lock.json" -packageName "vscode-stable"; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
