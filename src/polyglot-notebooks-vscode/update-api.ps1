[CmdletBinding(PositionalBinding = $false)]
param (
    [string]$version
)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

try {
    function DownloadVsCodeApi([string] $branchName, [string] $destinationDirectory, [bool] $isInsiders = $false) {
        Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vscode-dts/vscode.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.d.ts"
        Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vscode-dts/vscode.proposed.notebookMessaging.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.proposed.notebookMessaging.d.ts"
        if ($isInsiders) {
             Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vscode-dts/vscode.proposed.languageConfigurationAutoClosingPairs.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.proposed.languageConfigurationAutoClosingPairs.d.ts"
        }

    }

    # stable
    DownloadVsCodeApi -branchName "release/$version" -destinationDirectory "src"

    # insiders
    DownloadVsCodeApi -branchName "main" -destinationDirectory "..\polyglot-notebooks-vscode-insiders\src" -isInsiders $true
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
