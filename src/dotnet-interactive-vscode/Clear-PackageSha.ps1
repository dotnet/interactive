# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

[CmdletBinding(PositionalBinding=$false)]
param (
    [string]$projectJsonLockPath,
    [string]$packageName
)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

try {
    $packageJsonLockContents = Get-Content $projectJsonLockPath | Out-String | ConvertFrom-Json
    $packageJsonLockContents."dependencies"."$packageName".PSObject.Properties.Remove("integrity")
    $packageJsonLockContents | ConvertTo-Json -depth 100 | Out-File $projectJsonLockPath
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
