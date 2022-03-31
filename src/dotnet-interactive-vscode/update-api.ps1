$vsCodeStableVersion = (Get-Content "$PSScriptRoot\vscodeStableVersion.txt").Trim()

function DownloadVsCodeApi([string] $branchName, [string] $destinationDirectory) {
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vscode-dts/vscode.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.d.ts"
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vscode-dts/vscode.proposed.notebookConcatTextDocument.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.proposed.notebookConcatTextDocument.d.ts"
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vscode-dts/vscode.proposed.notebookCellExecutionState.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.proposed.notebookCellExecutionState.d.ts"
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vscode-dts/vscode.proposed.notebookControllerKind.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.proposed.notebookControllerKind.d.ts"
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vscode-dts/vscode.proposed.notebookDebugOptions.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.proposed.notebookDebugOptions.d.ts"
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vscode-dts/vscode.proposed.notebookDeprecated.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.proposed.notebookDeprecated.d.ts"
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vscode-dts/vscode.proposed.notebookEditor.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.proposed.notebookEditor.d.ts"
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vscode-dts/vscode.proposed.notebookEditorDecorationType.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.proposed.notebookEditorDecorationType.d.ts"
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vscode-dts/vscode.proposed.notebookEditorEdit.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.proposed.notebookEditorEdit.d.ts"
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vscode-dts/vscode.proposed.notebookLiveShare.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.proposed.notebookLiveShare.d.ts"
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vscode-dts/vscode.proposed.notebookMessaging.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.proposed.notebookMessaging.d.ts"
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vscode-dts/vscode.proposed.notebookMime.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.proposed.notebookMime.d.ts"
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vscode-dts/vscode.proposed.textDocumentNotebook.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.proposed.textDocumentNotebook.d.ts"
}

function DownloadLegacyVsCodeApi([string] $branchName, [string] $destinationDirectory) {    
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vs/vscode.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.d.ts"
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vs/vscode.proposed.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.proposed.d.ts"
}

# stable
DownloadVsCodeApi -branchName "release/$vsCodeStableVersion" -destinationDirectory "src"

# insiders
DownloadVsCodeApi -branchName "main" -destinationDirectory "..\dotnet-interactive-vscode-insiders\src"

# azure data studio
DownloadLegacyVsCodeApi -branchName "release/1.59" -destinationDirectory "..\dotnet-interactive-vscode-ads\src"
