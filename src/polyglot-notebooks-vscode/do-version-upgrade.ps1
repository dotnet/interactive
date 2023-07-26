[CmdletBinding(PositionalBinding = $false)]
param (
    [string]$version
)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

try {
    $stableDirectory = Resolve-Path $PSScriptRoot
    $insidersDirectory = Resolve-Path "$PSScriptRoot\..\polyglot-notebooks-vscode-insiders"

    # ensure semantic token types are up to date
    Push-Location $insidersDirectory
    & node .\tools\buildSemanticTokenScopes.js
    Pop-Location

    # copy package.json from insider
    Copy-Item -Path "$insidersDirectory\package.json" -Destination  "$stableDirectory\package.json"

    $stablePackageJsonContents = (Get-Content "$stableDirectory\package.json" | Out-String | ConvertFrom-Json)

    $stablePackageJsonContents.scripts.package = $stablePackageJsonContents.scripts.package.Replace("--pre-release","").Trim()

    # ensure the stable is using the available proposed apis
    $stablePackageJsonContents.enabledApiProposals = @("notebookMessaging")

    $stablePackageJsonContents | ConvertTo-Json -depth 100 | Out-File "$stableDirectory\package.json"

    # copy grammar files
    Remove-Item -Path "$stableDirectory\grammars\*"
    Copy-Item -Path "$insidersDirectory\grammars\*" -Destination "$stableDirectory\grammars\"

    # copy help articles
    Remove-Item -Path "$stableDirectory\help\*"
    Copy-Item -Path "$insidersDirectory\help\*" -Destination "$stableDirectory\help\"

    # copy source files
    Remove-Item -Path "$stableDirectory\src\*" -Filter "*.ts"
    Copy-Item -Path "$insidersDirectory\src\*" -Destination "$stableDirectory\src\" -Filter "*.ts"

    # copy localization files
    Remove-Item -Path "$stableDirectory\*" -Filter "package.nls.*"
    Copy-Item -Path "$insidersDirectory\*" -Destination "$stableDirectory\" -Filter "package.nls.*"
    Remove-Item -Path "$stableDirectory\l10n\*" -Filter "bundle.l10n.*"
    Copy-Item -Path "$insidersDirectory\l10n\*" -Destination "$stableDirectory\l10n\" -Filter "bundle.l10n.*"

    # update apis
    . "$PSScriptRoot\update-api.ps1" -version $version

    # update version numbers
    . "$PSScriptRoot\update-versions.ps1" -updateAll -version $version
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
