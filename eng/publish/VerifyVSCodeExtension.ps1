[CmdletBinding(PositionalBinding=$false)]
param (
    [string]$extensionPath
)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

try {
    # create temporary location
    $guid = [System.Guid]::NewGuid().ToString()
    $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) $guid
    New-Item -ItemType Directory -Path $tempDir | Out-Null

    # extract to temporary location
    Expand-Archive -Path $extensionPath -DestinationPath $tempDir

    # read manifest
    $packageJsonPath = Join-Path $tempDir "extension" "package.json"
    $packageJsonContents = (Get-Content $packageJsonPath | Out-String | ConvertFrom-Json)

    # get package feed url
    $toolFeed = $packageJsonContents."contributes"."configuration"."properties"."dotnet-interactive.interactiveToolSource"."default"
    $serviceDefinition = Invoke-RestMethod -Uri $toolFeed
    $queryUrl = ($serviceDefinition."resources" | Where-Object -Property "@type" -eq "SearchQueryService" | Select-Object -First 1)."@id"
    $packageQuery = $queryUrl + "?q=Microsoft.dotnet-interactive"
    $packageQueryResults = Invoke-RestMethod -Uri $packageQuery
    $availableVersions = ($packageQueryResults."data" | Select-Object -First 1)."versions" | ForEach-Object { $_."version" }

    # ensure package exists
    $expectedVersion = $packageJsonContents."contributes"."configuration"."properties"."dotnet-interactive.minimumInteractiveToolVersion"."default"
    $exists = $availableVersions -contains $expectedVersion

    # cleanup unpacked extension
    Remove-Item -Path $tempDir -Recurse

    if ($exists) {
        Write-Host "Package version $expectedVersion exists on feed $toolFeed"
    } else {
        Write-Host "Package version $expectedVersion not found on feed $toolFeed"
        exit 1
    }
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
