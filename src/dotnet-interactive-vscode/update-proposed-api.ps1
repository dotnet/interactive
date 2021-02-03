$branchName = "release/1.53"

Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vs/vscode.d.ts" -OutFile "$PSScriptRoot\src\vscode\vscode.d.ts"
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/microsoft/vscode/$branchName/src/vs/vscode.proposed.d.ts" -OutFile "$PSScriptRoot\src\vscode\vscode.proposed.d.ts"
