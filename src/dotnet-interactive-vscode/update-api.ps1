$vsCodeStableVersion = (Get-Content "$PSScriptRoot\vscodeStableVersion.txt").Trim()

function DownloadVsCodeApi([string] $branchName, [string] $destinationDirectory, [bool] $isInsiders = $false) {
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vscode-dts/vscode.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.d.ts"
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vscode-dts/vscode.proposed.notebookMessaging.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.proposed.notebookMessaging.d.ts"
}

# stable
DownloadVsCodeApi -branchName "release/$vsCodeStableVersion" -destinationDirectory "src"

# insiders
DownloadVsCodeApi -branchName "main" -destinationDirectory "..\dotnet-interactive-vscode-insiders\src" -isInsiders $true
