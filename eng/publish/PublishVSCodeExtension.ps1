[CmdletBinding(PositionalBinding = $false)]
param (
    [string]$artifactsPath,
    [string[]]$vscodeTargets,
    [string]$vscodeToken,
    [string]$nugetToken,
    [switch]$simulate
)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

try {
    npm install -g @vscode/vsce
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    $vscodeTargets | ForEach-Object {
        $vscodeTarget = $_

        # find extension vsix
        $extension = Get-ChildItem "$artifactsPath\vscode\$vscodeTarget\dotnet-interactive-vscode-*.vsix" | Select-Object -First 1

        # verify
        if (-Not $simulate) {
            . "$PSScriptRoot\VerifyVSCodeExtension.ps1" -extensionPath $extension
            if ($LASTEXITCODE -ne 0) {
                exit $LASTEXITCODE
            }
        }

        # publish nuget
        if ($vscodeTarget -eq "stable") {
            $packagestoPublish = @(
                "Microsoft.dotnet-interactive",
                "Microsoft.DotNet.Interactive",
                "Microsoft.DotNet.Interactive.AIUtilities",
                "Microsoft.DotNet.Interactive.AspNetCore",                
                "Microsoft.DotNet.Interactive.Browser",
                "Microsoft.DotNet.Interactive.CSharp",
                "Microsoft.DotNet.Interactive.Documents",
                "Microsoft.DotNet.Interactive.ExtensionLab",
                "Microsoft.DotNet.Interactive.Formatting",
                "Microsoft.DotNet.Interactive.FSharp",
                "Microsoft.DotNet.Interactive.Http,",
                "Microsoft.DotNet.Interactive.Journey",
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
                $nugetPacakgeName = $_.Name
                if ($nugetPacakgeName -match '(?<=(?<id>.+))\.(?<version>((\d+\.\d+(\.\d+)?))(?<suffix>(-.*)?))\.nupkg')
                {
                    $packageId = $Matches.id
                    $packageVersion = $Matches.version

                    # publish only listed packages
                    if ($packagestoPublish.Contains($packageId)) {
                        Write-Host "Publishing $nugetPackagePath"
                        if (-Not $simulate) {
                            dotnet nuget push $nugetPackagePath --source https://api.nuget.org/v3/index.json --api-key $nugetToken --no-symbols
                            if ($LASTEXITCODE -ne 0) {
                                exit $LASTEXITCODE
                            }
                        }
                    }
                }
            }
        }

        # publish vs code marketplace
        Write-Host "Publishing $extension"
        if (-Not $simulate) {
            if ($vscodeTarget -eq "insiders") {
                vsce publish --pre-release --packagePath $extension --pat $vscodeToken --noVerify
            }
            else{
                vsce publish --packagePath $extension --pat $vscodeToken --noVerify
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
