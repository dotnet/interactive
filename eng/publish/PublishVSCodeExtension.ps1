[CmdletBinding(PositionalBinding = $false)]
param (
    [string]$artifactsPath,
    [string[]]$vscodeTargets,
    [string]$nugetToken,
    [switch]$simulate
)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

try {
    # Install vsce globally if not already installed
    npm install -g @vscode/vsce
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    $vscodeTargets | ForEach-Object {
        $vscodeTarget = $_

        # find extension vsix
        $extension = Get-ChildItem "$artifactsPath\vscode\$vscodeTarget\dotnet-interactive-vscode-*.vsix" | Select-Object -First 1

        # Verify the extension
        if (-Not $simulate) {
            . "$PSScriptRoot\VerifyVSCodeExtension.ps1" -extensionPath $extension
            if ($LASTEXITCODE -ne 0) {
                exit $LASTEXITCODE
            }
        }

        # Publish to NuGet (only for "stable" target)
        if ($vscodeTarget -eq "stable") {
            $packagestoPublish = @(
                "Microsoft.dotnet-interactive",
                "Microsoft.DotNet.Interactive",
                "Microsoft.DotNet.Interactive.AspNetCore",                
                "Microsoft.DotNet.Interactive.Browser",
                "Microsoft.DotNet.Interactive.CSharp",
                "Microsoft.DotNet.Interactive.Documents",
                "Microsoft.DotNet.Interactive.ExtensionLab",
                "Microsoft.DotNet.Interactive.Formatting",
                "Microsoft.DotNet.Interactive.FSharp",
                "Microsoft.DotNet.Interactive.Http",
                "Microsoft.DotNet.Interactive.Journey",
                "Microsoft.DotNet.Interactive.Jupyter",
                "Microsoft.DotNet.Interactive.Kql",
                "Microsoft.DotNet.Interactive.Mermaid",
                "Microsoft.DotNet.Interactive.NamedPipeConnector",
                "Microsoft.DotNet.Interactive.PackageManagement",
                "Microsoft.DotNet.Interactive.PowerShell",
                "Microsoft.DotNet.Interactive.SQLite",
                "Microsoft.DotNet.Interactive.SqlServer"
            )

            Get-ChildItem "$artifactsPath\packages\Shipping\Microsoft.DotNet*.nupkg" | ForEach-Object {
                $nugetPackagePath = $_.ToString()
                $nugetPackageName = $_.Name
                if ($nugetPackageName -match '(?<=(?<id>.+))\.(?<version>((\d+\.\d+(\.\d+)?))(?<suffix>(-.*)?))\.nupkg')
                {
                    $packageId = $Matches.id
                    $packageVersion = $Matches.version

                    # publish only listed packages
                    if ($packagestoPublish.Contains($packageId)) {
                        Write-Host "Publishing $nugetPackagePath"
                        if ($simulate) {
                            Write-Host "Simulated command: dotnet nuget push $nugetPackagePath --source https://api.nuget.org/v3/index.json --api-key $nugetToken --no-symbols"
                        } else {
                            dotnet nuget push $nugetPackagePath --source https://api.nuget.org/v3/index.json --api-key $nugetToken --no-symbols
                            if ($LASTEXITCODE -ne 0) {
                                exit $LASTEXITCODE
                            }
                        }
                    }
                }
            }
        }

        # Publish to VS Code Marketplace using Managed Identity
        Write-Host "Publishing $extension"
        if ($simulate) {
            if ($vscodeTarget -eq "insiders") {
                Write-Host "Simulated command: vsce publish --pre-release --packagePath $extension --noVerify --azure-credential"
            } else {
                Write-Host "Simulated command: vsce publish --packagePath $extension --noVerify --azure-credential"
            }
        } else {
            if ($vscodeTarget -eq "insiders") {
                vsce publish --pre-release --packagePath $extension --noVerify --azure-credential
            } else {
                vsce publish --packagePath $extension --noVerify --azure-credential
            }
        }
    }
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
