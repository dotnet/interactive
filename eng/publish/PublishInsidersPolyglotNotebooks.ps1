[CmdletBinding(PositionalBinding = $false)]
param (
    [string]$artifactsPath,
    [string]$nugetToken,
    [string]$isSimulated
)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

$simulate = if ($isSimulated -eq "true") { $true } else { $false }
Write-Host "Simulate switch is set to: $simulate"

try {
    Write-Host "vscode target: insiders"

    Write-Host "Find extension vsix..."
    $extension = Get-ChildItem "$artifactsPath\vscode\insiders\dotnet-interactive-vscode-*.vsix" | Select-Object -First 1
    Write-Host "Found extension: $extension"

    Write-Host "Verify the extension..."
    if ($simulate) {
        Write-Host "Simulated command: . '$PSScriptRoot\VerifyVSCodeExtension.ps1' -extensionPath $extension"
    } else {
        . "$PSScriptRoot\VerifyVSCodeExtension.ps1" -extensionPath $extension
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }

    Write-Host "Publishing extension $extension to VS Code Marketplace using Managed Identity..."
    if ($simulate) {
        Write-Host "Simulated command: vsce publish --pre-release --packagePath $extension --noVerify --azure-credential"
    } else {
        vsce publish --pre-release --packagePath $extension --noVerify --azure-credential 
    }
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
