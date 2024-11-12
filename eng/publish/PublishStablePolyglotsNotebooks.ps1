param (
    [string]$artifactsPath,
    [string]$nugetToken,
    [string]$isSimulated
)

Import-Module "$PSScriptRoot\PublishPolyglotNotebooksHelper.psm1"

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$simulate = $isSimulated -eq "true"
Write-Host "Simulate switch is set to: $simulate"

try {
    PublishInsidersExtension -artifactsPath $artifactsPath -simulate $simulate

    Write-Host "=========================================================================="

    PublishStableExtensionAndNuGetPackages -artifactsPath $artifactsPath -nugetToken $nugetToken -simulate $simulate
}
catch {
    Write-Host $_
    Write-Host $_.Exception.Message
    Write-Host $_.ScriptStackTrace
    exit 1
}
