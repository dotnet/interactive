[CmdletBinding(PositionalBinding = $false)]
param (
    [string]$stableToolVersionNumber,
    [string]$outDir
)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function Build-VsCodeExtension([string] $packageDirectory, [string] $outputSubDirectory, [string] $packageVersionNumber, [string] $kernelVersionNumber = "") {
    Push-Location $packageDirectory

    $packageJsonPath = Join-Path (Get-Location) "package.json"
    $packageJsonContents = ReadJson -packageJsonPath $packageJsonPath
    SetNpmVersionNumber -packageJsonContents $packageJsonContents -packageVersionNumber $packageVersionNumber

    # set tool version
    if ($kernelVersionNumber -Ne "") {
        Write-Host "Setting tool version to $kernelVersionNumber"
        $packageJsonContents.contributes.configuration.properties."dotnet-interactive.requiredInteractiveToolVersion"."default" = $kernelVersionNumber
    }

    SaveJson -packageJsonPath $packagejsonPath -packageJsonContents $packageJsonContents

    # create destination
    if ($outputSubDirectory -Eq "") {
        $outputSubDirectory = $packageDirectory
    }
    EnsureCleanDirectory -location "$outDir\$outputSubDirectory"

    # pack
    Write-Host "Packing extension"
    # Pass --allow-package-all-secrets & --allow-package-env-file because the @secretlint package used by vsce is attempting to read a directory as a file and this skip scanning.
    npx @vscode/vsce package -o "$outDir\$outputSubDirectory\dotnet-interactive-vscode-$packageVersionNumber.vsix" --allow-package-all-secrets --allow-package-env-file

    Write-Host "Generating extension manifest"
    npx @vscode/vsce generate-manifest -i "$outDir\$outputSubDirectory\dotnet-interactive-vscode-$packageVersionNumber.vsix" -o "$outDir\$outputSubDirectory\dotnet-interactive-vscode-$packageVersionNumber.manifest"

    Write-Host "Preparing manifest for signing"
    Copy-Item -Path "$outDir\$outputSubDirectory\dotnet-interactive-vscode-$packageVersionNumber.manifest" -Destination "$outDir\$outputSubDirectory\dotnet-interactive-vscode-$packageVersionNumber.signature.p7s"

    Pop-Location
}

try {
    . "$PSScriptRoot\PackUtilities.ps1"

    # copy publish scripts
    EnsureCleanDirectory -location $outDir
    Copy-Item -Path $PSScriptRoot\..\publish\* -Destination $outDir -Recurse

    $stablePackageVersion = "${stableToolVersionNumber}0"
    $insidersPackageVersion = "${stableToolVersionNumber}1"
    Build-VsCodeExtension -packageDirectory "polyglot-notebooks-vscode" -outputSubDirectory "stable-locked" -packageVersionNumber $stablePackageVersion
    Build-VsCodeExtension -packageDirectory "polyglot-notebooks-vscode" -outputSubDirectory "stable" -packageVersionNumber $stablePackageVersion -kernelVersionNumber $stableToolVersionNumber
    Build-VsCodeExtension -packageDirectory "polyglot-notebooks-vscode-insiders" -outputSubDirectory "insiders" -packageVersionNumber $insidersPackageVersion -kernelVersionNumber $stableToolVersionNumber
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
