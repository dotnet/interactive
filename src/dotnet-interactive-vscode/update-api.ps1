function DownloadApi([string] $branchName, [string] $destinationDirectory) {
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vs/vscode.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.d.ts"
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vs/vscode.proposed.d.ts" -OutFile "$PSScriptRoot\$destinationDirectory\vscode.proposed.d.ts"
}

$stableVersion = "1.53"

# common
DownloadApi -branchName "release/$stableVersion" -destinationDirectory "src\vscode"

# release specific
DownloadApi -branchName "master" -destinationDirectory "src\vscode\insiders\src"
DownloadApi -branchName "release/$stableVersion" -destinationDirectory "src\vscode\stable\src"
