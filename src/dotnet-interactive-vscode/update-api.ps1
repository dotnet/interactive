$vsCodeStableVersion = "1.54"
$jupyterStableVersion = "2021.02.1"

function DownloadVsCodeApi([string] $branchName, [string] $destinationDirectory) {
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vs/vscode.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.d.ts"
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vs/vscode.proposed.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.proposed.d.ts"
}

function DownloadJupyterApi([string] $branchName) {
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode-jupyter/$jupyterStableVersion/src/test/datascience/extensionapi/exampleextension/ms-ai-tools-test/src/typings/jupyter.d.ts" -OutFile "$PSScriptRoot\src\vscode\jupyter.d.ts"
}

# common
DownloadJupyterApi -branchName $jupyterStableVersion
DownloadVsCodeApi -branchName "release/$vsCodeStableVersion" -destinationDirectory "src\vscode"

# release specific
DownloadVsCodeApi -branchName "master" -destinationDirectory "src\vscode\insiders\src"
DownloadVsCodeApi -branchName "release/$vsCodeStableVersion" -destinationDirectory "src\vscode\stable\src"
