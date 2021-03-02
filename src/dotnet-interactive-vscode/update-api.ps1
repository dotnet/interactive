$vsCodeStableVersion = (Get-Content "$PSScriptRoot\vscodeStableVersion.txt").Trim()
$jupyterStableVersion = "2021.02.1"

function DownloadJupyterApi([string] $branchName, [string] $destinationDirectory) {
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode-jupyter/$jupyterStableVersion/src/test/datascience/extensionapi/exampleextension/ms-ai-tools-test/src/typings/jupyter.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\jupyter.d.ts"
}

function DownloadVsCodeApi([string] $branchName, [string] $destinationDirectory) {
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vs/vscode.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.d.ts"
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vs/vscode.proposed.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.proposed.d.ts"
}

# stable
DownloadJupyterApi -branchName $jupyterStableVersion -destinationDirectory "stable\src"
DownloadVsCodeApi -branchName "release/$vsCodeStableVersion" -destinationDirectory "stable\src"

# insiders
DownloadVsCodeApi -branchName "master" -destinationDirectory "insiders\src"
