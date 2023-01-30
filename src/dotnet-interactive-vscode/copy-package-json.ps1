Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

try {
    $stablePackageJsonPath = Join-Path $PSScriptRoot "package.json"
    $insidersPackageJsonPath = Join-Path $PSScriptRoot "..\dotnet-interactive-vscode-insiders\package.json"
    $packageJsonContents = (Get-Content $insidersPackageJsonPath | Out-String | ConvertFrom-Json)
    $commandLineArguments = $packageJsonContents.contributes.configuration.properties."dotnet-interactive.kernelTransportArgs".default
    $updatedCommandLineArguments = $commandLineArguments | Where-Object { $_ -ne "--preview" }
    $packagejsonContents.contributes.configuration.properties."dotnet-interactive.kernelTransportArgs".default = $updatedCommandLineArguments
    $packageJsonContents | ConvertTo-Json -depth 100 | Out-File $stablePackageJsonPath
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
