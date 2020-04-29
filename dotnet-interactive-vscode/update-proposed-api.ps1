$uri = "https://raw.githubusercontent.com/microsoft/vscode/master/src/vs/vscode.proposed.d.ts"
$local = "$PSScriptRoot\src\vscode\vscode.proposed.d.ts"
Invoke-WebRequest -Uri $uri -OutFile $local
