# get tool feed url...
$packageJsonPath = Join-Path ($PSScriptRoot) "package.json"
$packageJsonContents = (Get-Content $packageJsonPath | Out-String | ConvertFrom-Json)
$toolFeed = $packageJsonContents."contributes"."configuration"."properties"."dotnet-interactive.interactiveToolSource"."default"

# ...find latest tool version...
$serviceDefinition = Invoke-RestMethod -Uri $toolFeed
$queryUrl = ($serviceDefinition."resources" | Where-Object -Property "@type" -eq "SearchQueryService" | Select-Object -First 1)."@id"
$packageQuery = $queryUrl + "?q=Microsoft.dotnet-interactive"
$packageQueryResults = Invoke-RestMethod -Uri $packageQuery
$newToolVersion = ($packageQueryResults."data" | Select-Object -First 1)."version"

# ...and compare to existing
$existingToolVersion = $packageJsonContents."contributes"."configuration"."properties"."dotnet-interactive.minimumInteractiveToolVersion"."default"
if ($existingToolVersion -eq $newToolVersion) {
    Write-Host "Existing tool version $existingToolVersion is up to date."
} else {
    Write-Host "Updating tool version from $existingToolVersion to $newToolVersion"
    $packageJsonContents."contributes"."configuration"."properties"."dotnet-interactive.minimumInteractiveToolVersion"."default" = $newToolVersion
    $packageJsonContents | ConvertTo-Json -depth 100 | Out-File $packageJsonPath
}
