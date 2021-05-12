$vsCodeStableVersion = (Get-Content "$PSScriptRoot\vscodeStableVersion.txt").Trim()

function DownloadVsCodeApi([string] $branchName, [string] $destinationDirectory) {
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vs/vscode.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.d.ts"
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vs/vscode.proposed.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.proposed.d.ts"
}

# stable
DownloadVsCodeApi -branchName "release/$vsCodeStableVersion" -destinationDirectory "stable\src"

# insiders
DownloadVsCodeApi -branchName "main" -destinationDirectory "insiders\src"
