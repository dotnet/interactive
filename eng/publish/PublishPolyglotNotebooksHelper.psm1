function FindChildItem {
    param (
        [string]$pattern
    )

    $childItem = = Get-ChildItem $pattern | Select-Object -First 1
    Write-Host "Found extension: $childItem"

    $fileSize = $childItem.Length
    Write-Host "File Size: $fileSize bytes"

    $hash = Get-FileHash -Path $childItem.FullName -Algorithm SHA256
    $hashBytes = [System.Text.Encoding]::UTF8.GetBytes($hash.Hash)
    $base64Hash = [System.Convert]::ToBase64String($hashBytes)
    Write-Host "File Hash: $base64Hash"

    return $childItem
}

function PublishInsidersExtension {
    param (
        [string]$artifactsPath,
        [bool]$simulate
    )

    Write-Host "vscode target: insiders"

    Write-Host "Find extension vsix..."
    $extension = FindChildItem "$artifactsPath\vscode\insiders\dotnet-interactive-vscode-*.vsix" 

    Write-Host "Find extension manifest..."
    $manifest = FindChildItem "$artifactsPath\vscode\insiders\dotnet-interactive-vscode-*.manifest"

    Write-Host "Find extension signature..."
    $signature = FindChildItem "$artifactsPath\vscode\insiders\dotnet-interactive-vscode-*.signature.p7s"

    Write-Host "Verify the extension..."
    if ($simulate) {
        Write-Host "Simulated command: . '$PSScriptRoot\VerifyVSCodeExtension.ps1' -extensionPath $extension"
    } else {
        . "$PSScriptRoot\VerifyVSCodeExtension.ps1" -extensionPath $extension
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }

    Write-Host "Verify the extension signature..."
    $output = vsce verify-signature --packagePath $extension --manifestPath $manifest --signaturePath $signature

    # This is a brittle check but the command does not return a non-zero exit code for failed validation.
    # Opened https://github.com/microsoft/vscode-vsce/issues/1192 to track this.
    if ($output.Contains('Signature verification result: Success')) {
        Write-Host "Signature verification succeeded for $extension"
    } else {
        Write-Host ($output | Out-String)
        Write-Host "##[error]Signature verification failed for $extension"
        if (!$simulate) {
            exit 1
        }
    }

    Write-Host "Publishing extension $extension to VS Code Marketplace using Managed Identity..."
    if ($simulate) {
        Write-Host "Simulated command: vsce publish --pre-release --packagePath $extension --manifestPath $manifest --signaturePath $signature --noVerify --azure-credential"
    } else {
        vsce publish --pre-release --packagePath $extension --manifestPath $manifest --signaturePath $signature --noVerify --azure-credential
    }
}

function PublishStableExtensionAndNuGetPackages {
    param (
        [string]$artifactsPath,
        [string]$nugetToken,
        [bool]$simulate
    )

    Write-Host "vscode target: stable"

    Write-Host "Find extension vsix..."
    $extension = FindChildItem "$artifactsPath\vscode\stable\dotnet-interactive-vscode-*.vsix"

    Write-Host "Find extension manifest..."
    $manifest = FindChildItem "$artifactsPath\vscode\stable\dotnet-interactive-vscode-*.manifest"

    Write-Host "Find extension signature..."
    $signature = FindChildItem "$artifactsPath\vscode\stable\dotnet-interactive-vscode-*.signature.p7s"

    Write-Host "Verify the extension..."
    if ($simulate) {
        Write-Host "Simulated command: . '$PSScriptRoot\VerifyVSCodeExtension.ps1' -extensionPath $extension"
    } else {
        . "$PSScriptRoot\VerifyVSCodeExtension.ps1" -extensionPath $extension
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }

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
        "Microsoft.DotNet.Interactive.PostgreSQL",
        "Microsoft.DotNet.Interactive.PowerShell",
        "Microsoft.DotNet.Interactive.SQLite",
        "Microsoft.DotNet.Interactive.SqlServer",
        "Microsoft.DotNet.Interactive"
    )

    Get-ChildItem "$artifactsPath\packages\Shipping\*.nupkg" -Exclude '*.symbols.nupkg' | ForEach-Object {
        $nugetPackagePath = $_.FullName
        $nugetPackageName = $_.Name

        if ($nugetPackageName -match '^(?<id>.+?)\.(?<version>\d+\.\d+(\.\d+)?(-.*)?)\.nupkg$') {
            $packageId = $Matches['id']

            if ($packagestoPublish -contains $packageId) {
                Write-Host "Publishing NuGet package $nugetPackagePath"
                if ($simulate) {
                    Write-Host "Simulated command: dotnet nuget push $nugetPackagePath --source https://api.nuget.org/v3/index.json --api-key *** --no-symbols"
                } else {
                    dotnet nuget push $nugetPackagePath --source https://api.nuget.org/v3/index.json --api-key $nugetToken --no-symbols
                    if ($LASTEXITCODE -ne 0) {
                        exit $LASTEXITCODE
                    }
                }
            } else {
                Write-Host "Skipping publishing NuGet package $nugetPackagePath"
            }
        }
    }

    Write-Host "Publishing extension $extension to VS Code Marketplace using Managed Identity..."
    if ($simulate) {
        Write-Host "Simulated command: vsce publish --packagePath $extension --manifestPath $manifest --signaturePath $signature --noVerify --azure-credential"
    } else {
        vsce publish --packagePath $extension --manifestPath $manifest --signaturePath $signature --noVerify --azure-credential
    }
}
