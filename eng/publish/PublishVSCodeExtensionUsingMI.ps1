[CmdletBinding(PositionalBinding = $false)]
param (
    [string]$artifactsPath,
    [string]$vscodeTargetsList,
    [string]$nugetToken,
    [string]$isSimulated
)

$vscodeTargets = $vscodeTargetsList -split ','

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

$simulate = if ($isSimulated -eq "true") { $true } else { $false }
Write-Host "Simulate switch is set to: $simulate"

try {
    $vscodeTargets | ForEach-Object {
        $vscodeTarget = $_
        Write-Host "vscode target: $vscodeTarget"

        Write-Host "Find extension vsix..."
        $extension = Get-ChildItem "$artifactsPath\vscode\$vscodeTarget\dotnet-interactive-vscode-*.vsix" | Select-Object -First 1
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

        if ($vscodeTarget -eq "stable") {
            Write-Host "Publish to NuGet (only for 'stable' target)"

            $packagestoPublish = @(
                "Microsoft.dotnet-interactive",
                "Microsoft.DotNet.Interactive.AspNetCore",                
                "Microsoft.DotNet.Interactive.Browser",
                "Microsoft.DotNet.Interactive.CSharp",
                "Microsoft.DotNet.Interactive.Documents",
                "Microsoft.DotNet.Interactive.DuckDB",
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
                "Microsoft.DotNet.Interactive",
            )

            Get-ChildItem "$artifactsPath\packages\Shipping\*.nupkg" -Exclude '*.symbols.nupkg' | ForEach-Object {
                $nugetPackagePath = $_.ToString()
                $nugetPackageName = $_.Name

                if ($nugetPackageName -match '(?<=(?<id>.+))\.(?<version>((\d+\.\d+(\.\d+)?))(?<suffix>(-.*)?))\.nupkg')
                {
                    $packageId = $Matches.id

                    Write-Host "Publish only listed packages..."
                    if ($packagestoPublish.Contains($packageId)) {
                        Write-Host "Publishing nuget package $nugetPackagePath"
                        if ($simulate) {
                            Write-Host "Simulated command: dotnet nuget push $nugetPackagePath --source https://api.nuget.org/v3/index.json --api-key $nugetToken --no-symbols"
                        } else {
                            dotnet nuget push $nugetPackagePath --source https://api.nuget.org/v3/index.json --api-key $nugetToken --no-symbols
                            if ($LASTEXITCODE -ne 0) {
                                exit $LASTEXITCODE
                            }
                        }
                    } else {
                        Write-Host "Skipping publishing nuget package $nugetPackagePath"
                    }
                }
            }
        }

        Write-Host "Publishing extension $extension to VS Code Marketplace using Managed Identity..."
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
