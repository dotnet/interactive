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

    # copy package.json
    Copy-Item -Path "$stableDirectory\package.json" -Destination "$insidersDirectory\package.json"

    $insidersPackageJsonContents = (Get-Content "$insidersDirectory\package.json" | Out-String | ConvertFrom-Json)

    $insidersPackageJsonContents.scripts.package += " --pre-release" 

    $insidersPackageJsonContents | ConvertTo-Json -depth 100 | Out-File "$insidersDirectory\package.json"

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
