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
function doWork ([string] $ciSwitch){
    try {
        if ($ciSwitch -eq "-ci") {

            $sqlVersion="3.0.0-release.52"
            $downloads=(Join-Path $PSScriptRoot "..\artifacts\downloads")

            . (Join-Path $PSScriptRoot "DownLoadSqlToolsService.ps1") Release -out $downloads -version "$sqlVersion"
            if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        }

        # invoke regular build/test script
        . (Join-Path $PSScriptRoot "common\build.ps1") -projects "$PSScriptRoot\..\dotnet-interactive.sln" @arguments
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
    catch {
        Write-Host $_
        Write-Host $_.Exception
        Write-Host $_.ScriptStackTrace
        exit 1
    }
}

doWork ($args | Select-String -Pattern '-ci' -CaseSensitive -SimpleMatch)