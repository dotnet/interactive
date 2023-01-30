[CmdletBinding(PositionalBinding = $false)]
param (
    [string]$version
)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

try {
    $stableDirectory = Resolve-Path $PSScriptROot
    $insidersDirectory = Resolve-Path "$PSScriptRoot\..\dotnet-interactive-vscode-insiders"

    # copy and patch package.json
    . "$stableDirectory\copy-package-json.ps1"

    # copy grammar files
    Remove-Item -Path "$stableDirectory\grammars\*"
    Copy-Item -Path "$insidersDirectory\grammars\*" -Destination "$stableDirectory\grammars\"

    # copy help articles
    Remove-Item -Path "$stableDirectory\help\*"
    Copy-Item -Path "$insidersDirectory\help\*" -Destination "$stableDirectory\help\"

    # copy source files
    Remove-Item -Path "$stableDirectory\src\*" -Filter "*.ts"
    Copy-Item -Path "$insidersDirectory\src\*" -Destination "$stableDirectory\src\" -Filter "*.ts"

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
