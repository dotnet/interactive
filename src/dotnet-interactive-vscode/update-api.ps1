$vsCodeStableVersion = (Get-Content "$PSScriptRoot\vscodeStableVersion.txt").Trim()

function DownloadVsCodeApi([string] $branchName, [string] $destinationDirectory) {
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vscode-dts/vscode.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.d.ts"
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vscode-dts/vscode.proposed.notebookCellExecutionState.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.proposed.notebookCellExecutionState.d.ts"
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vscode-dts/vscode.proposed.notebookControllerKind.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.proposed.notebookControllerKind.d.ts"
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vscode-dts/vscode.proposed.notebookDebugOptions.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.proposed.notebookDebugOptions.d.ts"
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vscode-dts/vscode.proposed.notebookEditor.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.proposed.notebookEditor.d.ts"
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vscode-dts/vscode.proposed.notebookMessaging.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.proposed.notebookMessaging.d.ts"
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vscode-dts/vscode.proposed.notebookMime.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.proposed.notebookMime.d.ts"
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vscode-dts/vscode.proposed.notebookWorkspaceEdit.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.proposed.notebookWorkspaceEdit.d.ts"
}

function DownloadLegacyVsCodeApi([string] $branchName, [string] $destinationDirectory) {
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vs/vscode.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.d.ts"
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vs/vscode.proposed.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.proposed.d.ts"
}

function GetAzureDataStudioVSCodeVersion() {
    # e.g., fetch the value "1.59.0" and turn it into "1.59"
    $fullVersion = (Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/azuredatastudio/main/product.json" | ConvertFrom-Json).vscodeVersion
    $versionParts = $fullVersion.Split(".")
    return "$($versionParts[0]).$($versionParts[1])"
}

# stable
DownloadVsCodeApi -branchName "release/$vsCodeStableVersion" -destinationDirectory "src"

# insiders
DownloadVsCodeApi -branchName "main" -destinationDirectory "..\dotnet-interactive-vscode-insiders\src"

# azure data studio
$adsVscodeBaseVersion = GetAzureDataStudioVSCodeVersion
DownloadLegacyVsCodeApi -branchName "release/$adsVscodeBaseVersion" -destinationDirectory "..\dotnet-interactive-vscode-ads\src"
