[CmdletBinding(PositionalBinding = $false)]
param (
    [string]$artifactsPath,
    [string[]]$vscodeTargets,
    [string]$vscodeToken,
    [string]$nugetToken
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
        $extension = Get-ChildItem "$artifactsPath\vscode\$vscodeTarget\dotnet-interactive-vscode-*.vsix" | Select-Object -First 1

        # verify
        . "$PSScriptRoot\VerifyVSCodeExtension.ps1" -extensionPath $extension
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }

        # publish nuget
        if ($vscodeTarget -eq "stable") {
            $nugetPackages = @(
                'Microsoft.dotnet-interactive',
                'Microsoft.DotNet.Interactive',
                'Microsoft.DotNet.Interactive.CSharp',
                'Microsoft.DotNet.Interactive.Documents',
                'Microsoft.DotNet.Interactive.ExtensionLab',
                'Microsoft.DotNet.Interactive.Formatting',
                'Microsoft.DotNet.Interactive.FSharp',
                'Microsoft.DotNet.Interactive.Http',
                'Microsoft.DotNet.Interactive.Journey',
                'Microsoft.DotNet.Interactive.Kql',
                'Microsoft.DotNet.Interactive.PackageManagement',
                'Microsoft.DotNet.Interactive.PowerShell',
                'Microsoft.DotNet.Interactive.SqlServer'
            )
            $nugetPackages | ForEach-Object {
                $nugetPackagePath = "$artifactsPath\packages\Shipping\$_.*.nupkg"
                dotnet nuget push $nugetPackagePath --source https://api.nuget.org/v3/index.json --api-key $nugetToken --no-symbols 1
                if ($LASTEXITCODE -ne 0) {
                    exit $LASTEXITCODE
                }
            }
        }

        # publish vs code marketplace
        vsce publish --packagePath $extension --pat $vscodeToken --noVerify
    }
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
