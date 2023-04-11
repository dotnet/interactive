[CmdletBinding(PositionalBinding = $false)]
param (
    [switch]$updateAll = $false,
    [string]$version
)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

try {
    function Update-VersionNumbers([string] $packageJsonPath, [string] $vscodeEngine, [bool] $updateToolVersion) {
        Write-Host "Updating file '$packageJsonPath'"

        $packageJsonContents = (Get-Content $packageJsonPath | Out-String | ConvertFrom-Json)

        if ($updateToolVersion) {
            # get tool feed url...
            $toolFeed = $packageJsonContents."contributes"."configuration"."properties"."dotnet-interactive.interactiveToolSource"."default"
            Write-Host "Using tool feed $toolFeed"

            # ...find latest tool version...
            $serviceDefinition = Invoke-RestMethod -Uri $toolFeed
            $queryUrl = ($serviceDefinition."resources" | Where-Object -Property "@type" -Match ".*SearchQueryService.*" | Select-Object -First 1)."@id"
            $packageQuery = $queryUrl + "?q=Microsoft.dotnet-interactive"
            Write-Host "Using package query URL $packageQuery"
            $packageQueryResults = Invoke-RestMethod -Uri $packageQuery
            $newToolVersion = ($packageQueryResults."data" | Select-Object -First 1)."version"

            # ...compare to existing...
            $existingToolVersion = $packageJsonContents."contributes"."configuration"."properties"."dotnet-interactive.requiredInteractiveToolVersion"."default"
            if ($existingToolVersion -eq $newToolVersion) {
                Write-Host "Existing tool version $existingToolVersion is up to date."
            }
            else {
                Write-Host "Updating tool version from $existingToolVersion to $newToolVersion"
                $packageJsonContents."contributes"."configuration"."properties"."dotnet-interactive.requiredInteractiveToolVersion"."default" = $newToolVersion
            }
        }

        # ...update target VS Code engine...
        Write-Host "Setting VS Code engine to $vscodeEngine"
        $packageJsonContents."engines"."vscode" = $vscodeEngine

        # ...and save changes
        $packageJsonContents | ConvertTo-Json -depth 100 | Out-File $packageJsonPath
        Write-Host
    }    

    Update-VersionNumbers -packageJsonPath "$PSScriptRoot\package.json" -vscodeEngine "^$version.0" -updateToolVersion $updateAll
    Update-VersionNumbers -packageJsonPath "$PSScriptRoot\..\polyglot-notebooks-vscode-insiders\package.json" -vscodeEngine "^$version.0" -updateToolVersion $true
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
