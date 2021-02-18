# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

try {
    # Build-Interfaces
    Push-Location "$PSScriptRoot/src/interfaces"
    npm install; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    npm run compile; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    npm pack; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Pop-Location

    # Build insiders
    Push-Location "$PSScriptRoot/src/vscode/insiders"
    npm install; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    npm run compile; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    npm pack; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Pop-Location

    # Build stable
    Push-Location "$PSScriptRoot/src/vscode/stable"
    npm install; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    npm run compile; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    npm pack; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Pop-Location
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
