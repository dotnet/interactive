Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function TestUsingNPM([string] $testPath) {
    Write-Host "Installing packages"
    Start-Process -PassThru -WindowStyle Hidden -WorkingDirectory $testPath -Wait npm "i"
    Write-Host "Testing"
    $test = Start-Process -PassThru -WindowStyle Hidden -WorkingDirectory $testPath -Wait npm "run ciTest"
    Write-Host "Done with code $($test.ExitCode)"
    return $test.ExitCode
}

$arguments = $args
function isCi {
    $isCi = $arguments | Select-String -Pattern '-ci' -CaseSensitive -SimpleMatch
    return ($isCi -ne "")
}
$isCi = isCi

function buildConfiguration {
    $release = $arguments | Select-String -Pattern ('release', 'debug') -SimpleMatch -CaseSensitive
    if ([System.String]::IsNullOrWhitespace($release) -eq $true) {
        return "Debug"
    }
    else {
        return "$release"
    }
}
$buildConfiguration = buildConfiguration

try {
    if (isCi -eq $true) {
        . (Join-Path $PSScriptRoot "..\buildSqlTools.cmd") $buildConfiguration
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }

    # invoke regular build/test script
    . (Join-Path $PSScriptRoot "common\build.ps1") -projects "$PSScriptRoot\..\dotnet-interactive.sln" @args
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}