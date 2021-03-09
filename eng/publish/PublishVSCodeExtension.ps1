[CmdletBinding(PositionalBinding = $false)]
param (
    [string]$artifactsPath,
    [string[]]$vscodeTargets,
    [string]$publishToken
)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

try {
    npm install -g vsce
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    $vscodeTargets | ForEach-Object {
        $vscodeTarget = $_

        # find extension vsix
        $extension = Get-ChildItem "$artifactsPath\$vscodeTarget\dotnet-interactive-vscode-*.vsix" | Select-Object -First 1

        # verify
        . "$PSScriptRoot\VerifyVSCodeExtension.ps1" -extensionPath $extension
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }

        # publish
        vsce publish --packagePath $extension --pat $publishToken --noVerify
    }
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
