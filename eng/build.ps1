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

try {
    # invoke regular build/test script
    . (Join-Path $PSScriptRoot "common\build.ps1") "/p:Projects=$PSScriptRoot\..\dotnet-interactive.sln;$PSScriptRoot\..\samples\extensions\SampleExtensions.sln" @args
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    # directly invoke npm tests
    # if (($null -ne $args) -and ($args.Contains("-test") -or $args.Contains("-t"))) {
    #     TestUsingNPM "$PSScriptRoot\..\Microsoft.DotNet.Try.Client"
    #     TestUsingNPM "$PSScriptRoot\..\Microsoft.DotNet.Try.js"
    # }
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
