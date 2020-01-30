param (
    [string]$packagePath,
    [parameter(ValueFromRemainingArguments=$true)][string[]]$filesToStrip
)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

try {
    $packageExpandedPath = "$env:TEMP\" + [System.Guid]::NewGuid()
    New-Item -ItemType Directory -Path $packageExpandedPath
    Expand-Archive -LiteralPath $packagePath -DestinationPath $packageExpandedPath
    $filesToStrip | ForEach-Object {
        $fileToStrip = $_
        Remove-Item "$packageExpandedPath\$fileToStrip"
    }
    Compress-Archive -Path "$packageExpandedPath\*" -DestinationPath $packagePath -Force
    Remove-Item -Path $packageExpandedPath -Recurse
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
